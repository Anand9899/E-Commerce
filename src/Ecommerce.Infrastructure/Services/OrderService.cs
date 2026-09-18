using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Infrastructure.Data;

namespace Ecommerce.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly ICouponService _couponService;
        private readonly IEmailService _emailService;

        public OrderService(ApplicationDbContext context, ICartService cartService, ICouponService couponService, IEmailService emailService)
        {
            _context = context;
            _cartService = cartService;
            _couponService = couponService;
            _emailService = emailService;
        }

        public async Task<int> PlaceOrderAsync(CheckoutVM model, string userId, string? guestSessionId = null)
        {
            CartVM cart;

            if (model.DirectProductId.HasValue && model.DirectProductId.Value > 0)
            {
                var product = await _context.Products
                    .Include(p => p.Variants)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == model.DirectProductId.Value);

                if (product == null)
                {
                    throw new InvalidOperationException("Selected product is no longer available.");
                }

                int directQty = Math.Max(1, model.DirectQuantity ?? 1);
                if (product.StockQuantity < directQty)
                {
                    throw new InvalidOperationException($"Insufficient stock for '{product.Name}'. Only {product.StockQuantity} available.");
                }

                decimal unitPrice = product.DiscountPrice ?? product.BasePrice;
                string? variantName = null;
                if (model.DirectVariantId.HasValue && model.DirectVariantId.Value > 0)
                {
                    var variant = product.Variants.FirstOrDefault(v => v.Id == model.DirectVariantId.Value);
                    if (variant != null)
                    {
                        unitPrice = variant.Price;
                        variantName = variant.VariantName;
                    }
                }

                string? primaryImg = product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl ?? product.Images.FirstOrDefault()?.ImageUrl;

                cart = new CartVM
                {
                    Items = new List<CartItemVM>
                    {
                        new CartItemVM
                        {
                            ProductId = product.Id,
                            ProductName = product.Name,
                            ProductSlug = product.Slug,
                            ImageUrl = primaryImg,
                            UnitPrice = unitPrice,
                            Quantity = directQty,
                            ProductVariantId = model.DirectVariantId,
                            VariantName = variantName,
                            AvailableStock = product.StockQuantity
                        }
                    }
                };
            }
            else
            {
                cart = await _cartService.GetCartAsync(userId, guestSessionId);
                if (cart.Items.Count == 0)
                {
                    throw new InvalidOperationException("Your cart is empty.");
                }

                // Verify stock availability
                foreach (var item in cart.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null || product.StockQuantity < item.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock for '{item.ProductName}'.");
                    }
                }
            }

            // Coupon calculation
            decimal discountAmount = 0;
            if (!string.IsNullOrWhiteSpace(cart.AppliedCouponCode))
            {
                var couponResult = await _couponService.ValidateAndCalculateCouponAsync(cart.AppliedCouponCode, cart.SubTotal, userId);
                if (couponResult.IsValid)
                {
                    discountAmount = couponResult.DiscountAmount;
                }
            }

            decimal subTotal = cart.SubTotal;
            decimal shippingFee = subTotal > 999 || subTotal == 0 ? 0 : 99;
            decimal taxAmount = Math.Round((subTotal - discountAmount) * 0.18m, 2);
            decimal grandTotal = Math.Max(0, subTotal - discountAmount + shippingFee + taxAmount);

            if (model.PaymentMethod == PaymentMethod.CashOnDelivery && grandTotal > 15000)
            {
                throw new InvalidOperationException("Cash on Delivery (COD) is not available for orders above ₹15,000. Please select an online payment option.");
            }



            // Generate unique Order Number
            var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check and calculate Wallet Deductions
                var user = !string.IsNullOrEmpty(userId) ? await _context.Users.FindAsync(userId) : null;
                decimal walletDeduct = 0;
                if (model.UseWalletBalance && user != null && user.WalletBalance > 0)
                {
                    walletDeduct = Math.Min(user.WalletBalance, grandTotal);
                    grandTotal -= walletDeduct;
                    user.WalletBalance -= walletDeduct;
                }

                // Calculate 5% Loyalty Reward Points earned on this order
                int loyaltyPointsEarned = (int)Math.Max(10, Math.Round(subTotal * 0.05m));
                if (user != null)
                {
                    user.LoyaltyPoints += loyaltyPointsEarned;
                }

                // If entire amount was paid via wallet
                var paymentMethod = model.PaymentMethod;
                var paymentStatus = model.PaymentMethod == PaymentMethod.CashOnDelivery ? PaymentStatus.Pending : PaymentStatus.Paid;
                if (grandTotal == 0 && walletDeduct > 0)
                {
                    paymentMethod = PaymentMethod.Wallet;
                    paymentStatus = PaymentStatus.Paid;
                }

                var order = new Order
                {
                    OrderNumber = orderNumber,
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    SubTotal = subTotal,
                    DiscountAmount = discountAmount,
                    ShippingFee = shippingFee,
                    TaxAmount = taxAmount,
                    GrandTotal = grandTotal,
                    Status = OrderStatus.Confirmed,
                    PaymentStatus = paymentStatus,
                    PaymentMethod = paymentMethod,
                    CouponCode = cart.AppliedCouponCode,
                    WalletAmountUsed = walletDeduct,
                    LoyaltyPointsEarned = loyaltyPointsEarned,
                    CustomerNotes = model.CustomerNotes,
                    ShippingFullName = model.ShippingFullName,
                    ShippingPhone = model.ShippingPhone,
                    ShippingAddressLine1 = model.ShippingAddressLine1,
                    ShippingAddressLine2 = model.ShippingAddressLine2,
                    ShippingCity = model.ShippingCity,
                    ShippingState = model.ShippingState,
                    ShippingPostalCode = model.ShippingPostalCode,
                    ShippingCountry = model.ShippingCountry,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                // If wallet funds were used, log wallet transaction
                if (walletDeduct > 0 && user != null)
                {
                    var walletTx = new WalletTransaction
                    {
                        UserId = userId,
                        Amount = walletDeduct,
                        Type = WalletTransactionType.Debit,
                        Description = $"Redeemed at Checkout for Order #{order.OrderNumber}",
                        BalanceAfter = user.WalletBalance,
                        OrderId = order.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.WalletTransactions.AddAsync(walletTx);
                }

                // Log Loyalty Points Reward
                if (user != null && loyaltyPointsEarned > 0)
                {
                    var pointsTx = new WalletTransaction
                    {
                        UserId = userId,
                        Amount = loyaltyPointsEarned,
                        Type = WalletTransactionType.Credit,
                        Description = $"Earned {loyaltyPointsEarned} Loyalty Reward Points on Order #{order.OrderNumber}",
                        BalanceAfter = user.WalletBalance,
                        OrderId = order.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.WalletTransactions.AddAsync(pointsTx);
                }

                // Order items and Inventory deduction
                foreach (var item in cart.Items)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        ProductVariantId = item.ProductVariantId,
                        ProductName = item.ProductName,
                        VariantName = item.VariantName,
                        SKU = item.ProductId.ToString(),
                        ProductImageUrl = item.ImageUrl,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        TotalPrice = item.TotalPrice,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.OrderItems.AddAsync(orderItem);

                    // Deduct stock & create stock transaction
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity -= item.Quantity;
                        if (item.ProductVariantId.HasValue && item.ProductVariantId.Value > 0)
                        {
                            var variant = await _context.ProductVariants.FindAsync(item.ProductVariantId.Value);
                            if (variant != null)
                            {
                                variant.StockQuantity -= item.Quantity;
                            }
                        }

                        var stockTx = new StockTransaction
                        {
                            ProductId = item.ProductId,
                            ProductVariantId = item.ProductVariantId,
                            QuantityChange = -item.Quantity,
                            RemainingStock = product.StockQuantity,
                            ChangeType = StockChangeType.Purchase,
                            Reference = $"Order {order.OrderNumber}",
                            CreatedAt = DateTime.UtcNow
                        };
                        await _context.StockTransactions.AddAsync(stockTx);
                    }
                }

                // Initial Status History
                var history = new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = OrderStatus.Confirmed,
                    Notes = "Order placed successfully.",
                    ChangedBy = "Customer",
                    CreatedAt = DateTime.UtcNow
                };
                await _context.OrderStatusHistories.AddAsync(history);

                // If coupon was applied, log usage
                if (!string.IsNullOrWhiteSpace(cart.AppliedCouponCode))
                {
                    var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == cart.AppliedCouponCode);
                    if (coupon != null)
                    {
                        coupon.UsedCount++;
                        var usage = new CouponUsage
                        {
                            CouponId = coupon.Id,
                            UserId = userId,
                            OrderId = order.Id,
                            DiscountAmount = discountAmount,
                            UsedAt = DateTime.UtcNow
                        };
                        await _context.CouponUsages.AddAsync(usage);
                    }
                }

                // If user wants to save address
                if (model.SaveAddressForFuture)
                {
                    var address = new Address
                    {
                        UserId = userId,
                        FullName = model.ShippingFullName,
                        PhoneNumber = model.ShippingPhone,
                        AddressLine1 = model.ShippingAddressLine1,
                        AddressLine2 = model.ShippingAddressLine2,
                        City = model.ShippingCity,
                        State = model.ShippingState,
                        PostalCode = model.ShippingPostalCode,
                        Country = model.ShippingCountry,
                        IsDefault = !(await _context.Addresses.AnyAsync(a => a.UserId == userId))
                    };
                    await _context.Addresses.AddAsync(address);
                }

                // Create Payment record
                var payment = new Payment
                {
                    OrderId = order.Id,
                    Amount = grandTotal,
                    Method = model.PaymentMethod,
                    Status = order.PaymentStatus,
                    TransactionId = !string.IsNullOrWhiteSpace(model.PaymentTransactionId)
                        ? model.PaymentTransactionId.Trim()
                        : (!string.IsNullOrWhiteSpace(model.UpiTransactionId)
                            ? model.UpiTransactionId.Trim()
                            : (model.PaymentMethod == PaymentMethod.UPI ? "UPI-" + DateTime.UtcNow.Ticks.ToString()[^8..] : (model.PaymentMethod == PaymentMethod.CreditOrDebitCard ? "CARD-" + Guid.NewGuid().ToString("N")[..10].ToUpper() : (model.PaymentMethod == PaymentMethod.NetBanking ? "NB-" + Guid.NewGuid().ToString("N")[..10].ToUpper() : "COD")))),
                    PaidAt = model.PaymentMethod != PaymentMethod.CashOnDelivery ? DateTime.UtcNow : null,
                    ProviderResponse = !string.IsNullOrWhiteSpace(model.PaymentGatewayResponse)
                        ? model.PaymentGatewayResponse
                        : (model.PaymentMethod == PaymentMethod.UPI
                            ? (!string.IsNullOrWhiteSpace(model.UpiTransactionId) ? $"UPI QR (UTR: {model.UpiTransactionId.Trim()})" : "UPI Payment Simulated")
                            : (model.PaymentMethod == PaymentMethod.CashOnDelivery ? "Cash On Delivery" : $"{model.PaymentMethod} Simulated Payment")),
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Payments.AddAsync(payment);

                await _context.SaveChangesAsync();

                // Clear user cart only for regular cart checkouts (keep cart intact for 1-click Direct Buy)
                if (!model.DirectProductId.HasValue || model.DirectProductId.Value <= 0)
                {
                    await _cartService.ClearCartAsync(userId, guestSessionId);
                }

                await transaction.CommitAsync();
                return order.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<OrderListVM>> GetUserOrdersAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Payments)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderListVM
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    GrandTotal = o.GrandTotal,
                    Status = o.Status,
                    PaymentStatus = o.PaymentStatus,
                    PaymentMethod = o.PaymentMethod,
                    TransactionId = o.Payments.OrderByDescending(p => p.CreatedAt).Select(p => p.TransactionId).FirstOrDefault(),
                    TotalItems = o.OrderItems.Sum(i => i.Quantity),
                    FirstProductImage = o.OrderItems.Select(i => i.ProductImageUrl).FirstOrDefault() ?? "/images/placeholder-product.png",
                    TrackingNumber = o.TrackingNumber,
                    CourierPartner = o.CourierPartner,
                    EstimatedDeliveryDate = o.EstimatedDeliveryDate
                })
                .ToListAsync();
        }

        public async Task<OrderDetailVM?> GetOrderDetailAsync(int orderId, string? userId = null)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .Include(o => o.Payments)
                .Include(o => o.StatusHistories.OrderByDescending(s => s.CreatedAt))
                .Where(o => o.Id == orderId);

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(o => o.UserId == userId);
            }

            var order = await query.FirstOrDefaultAsync();
            if (order == null) return null;

            var customerFullName = !string.IsNullOrWhiteSpace(order.User?.FullName)
                ? order.User.FullName
                : (!string.IsNullOrWhiteSpace(order.ShippingFullName) && !order.ShippingFullName.Contains("@") ? order.ShippingFullName : "Customer");

            return new OrderDetailVM
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                SubTotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                ShippingFee = order.ShippingFee,
                TaxAmount = order.TaxAmount,
                GrandTotal = order.GrandTotal,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                PaymentMethod = order.PaymentMethod,
                TransactionId = order.Payments.OrderByDescending(p => p.CreatedAt).Select(p => p.TransactionId).FirstOrDefault(),
                CouponCode = order.CouponCode,
                TrackingNumber = order.TrackingNumber,
                CourierPartner = order.CourierPartner,
                EstimatedDeliveryDate = order.EstimatedDeliveryDate,
                ShippedAt = order.ShippedAt,
                OutForDeliveryAt = order.OutForDeliveryAt,
                DeliveredAt = order.DeliveredAt,
                CurrentLocation = order.CurrentLocation,
                WalletAmountUsed = order.WalletAmountUsed,
                LoyaltyPointsEarned = order.LoyaltyPointsEarned,
                CustomerNotes = order.CustomerNotes,
                ReturnReason = order.ReturnReason,
                ReturnComments = order.ReturnComments,
                ReturnRefundPaymentDetails = order.ReturnRefundPaymentDetails,
                ReturnRequestedAt = order.ReturnRequestedAt,
                ReturnActionAt = order.ReturnActionAt,
                RefundAmount = order.RefundAmount,
                RefundReferenceId = order.RefundReferenceId,
                RefundAdminNotes = order.RefundAdminNotes,
                CustomerName = customerFullName,
                CustomerEmail = order.User?.Email,
                ShippingFullName = !string.IsNullOrWhiteSpace(order.ShippingFullName) && !order.ShippingFullName.Contains("@") ? order.ShippingFullName : customerFullName,
                ShippingPhone = order.ShippingPhone,
                ShippingAddressLine1 = order.ShippingAddressLine1,
                ShippingAddressLine2 = order.ShippingAddressLine2,
                ShippingCity = order.ShippingCity,
                ShippingState = order.ShippingState,
                ShippingPostalCode = order.ShippingPostalCode,
                ShippingCountry = order.ShippingCountry,
                Items = order.OrderItems.Select(i => new OrderItemVM
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    VariantName = i.VariantName,
                    SKU = i.SKU,
                    ProductImageUrl = i.ProductImageUrl,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    TotalPrice = i.TotalPrice
                }).ToList(),
                StatusHistories = order.StatusHistories.Select(sh => new OrderStatusHistoryVM
                {
                    Status = sh.Status,
                    ChangedAt = sh.CreatedAt,
                    Notes = sh.Notes,
                    ChangedBy = sh.ChangedBy
                }).ToList()
            };
        }

        public async Task<OrderDetailVM?> GetOrderByNumberAsync(string orderNumber, string? userId = null)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
            if (order == null) return null;
            return await GetOrderDetailAsync(order.Id, userId);
        }

        public async Task<List<OrderListVM>> GetAllAdminOrdersAsync(OrderStatus? statusFilter = null)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Payments)
                .AsQueryable();

            if (statusFilter.HasValue)
            {
                query = query.Where(o => o.Status == statusFilter.Value);
            }

            return await query
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderListVM
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    GrandTotal = o.GrandTotal,
                    Status = o.Status,
                    PaymentStatus = o.PaymentStatus,
                    PaymentMethod = o.PaymentMethod,
                    TransactionId = o.Payments.OrderByDescending(p => p.CreatedAt).Select(p => p.TransactionId).FirstOrDefault(),
                    TotalItems = o.OrderItems.Sum(i => i.Quantity),
                    FirstProductImage = o.OrderItems.Select(i => i.ProductImageUrl).FirstOrDefault() ?? "/images/placeholder-product.png",
                    TrackingNumber = o.TrackingNumber,
                    CourierPartner = o.CourierPartner,
                    EstimatedDeliveryDate = o.EstimatedDeliveryDate
                })
                .ToListAsync();
        }

        public async Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, string? notes, string changedBy)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return;

            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            if (newStatus == OrderStatus.Shipped && order.ShippedAt == null)
            {
                order.ShippedAt = DateTime.UtcNow;
                if (!order.EstimatedDeliveryDate.HasValue)
                {
                    order.EstimatedDeliveryDate = DateTime.UtcNow.AddDays(3);
                }
            }
            else if (newStatus == OrderStatus.OutForDelivery && order.OutForDeliveryAt == null)
            {
                order.OutForDeliveryAt = DateTime.UtcNow;
            }
            else if (newStatus == OrderStatus.Delivered && order.DeliveredAt == null)
            {
                order.DeliveredAt = DateTime.UtcNow;
                if (order.PaymentStatus == PaymentStatus.Pending && order.PaymentMethod == PaymentMethod.CashOnDelivery)
                {
                    order.PaymentStatus = PaymentStatus.Paid;
                }
            }

            var history = new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = newStatus,
                Notes = notes ?? $"Status updated to {newStatus}",
                ChangedBy = changedBy,
                CreatedAt = DateTime.UtcNow
            };

            await _context.OrderStatusHistories.AddAsync(history);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateShipmentTrackingAsync(UpdateShipmentTrackingDto dto, string changedBy)
        {
            var order = await _context.Orders.FindAsync(dto.OrderId);
            if (order == null) return;

            order.Status = dto.Status;
            order.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dto.TrackingNumber))
            {
                order.TrackingNumber = dto.TrackingNumber.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.CourierPartner))
            {
                order.CourierPartner = dto.CourierPartner.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.CurrentLocation))
            {
                order.CurrentLocation = dto.CurrentLocation.Trim();
            }

            if (dto.EstimatedDeliveryDate.HasValue)
            {
                order.EstimatedDeliveryDate = dto.EstimatedDeliveryDate.Value;
            }

            if (dto.Status == OrderStatus.Shipped)
            {
                order.ShippedAt ??= DateTime.UtcNow;
                if (!order.EstimatedDeliveryDate.HasValue)
                {
                    order.EstimatedDeliveryDate = DateTime.UtcNow.AddDays(3);
                }
            }
            else if (dto.Status == OrderStatus.OutForDelivery)
            {
                order.OutForDeliveryAt ??= DateTime.UtcNow;
            }
            else if (dto.Status == OrderStatus.Delivered)
            {
                order.DeliveredAt ??= DateTime.UtcNow;
                if (order.PaymentStatus == PaymentStatus.Pending && order.PaymentMethod == PaymentMethod.CashOnDelivery)
                {
                    order.PaymentStatus = PaymentStatus.Paid;
                }
            }

            var notesBuilder = new System.Text.StringBuilder();
            notesBuilder.Append($"Shipment tracking updated: Status={dto.Status}");
            if (!string.IsNullOrWhiteSpace(dto.CourierPartner)) notesBuilder.Append($", Courier={dto.CourierPartner}");
            if (!string.IsNullOrWhiteSpace(dto.TrackingNumber)) notesBuilder.Append($", AWB={dto.TrackingNumber}");
            if (!string.IsNullOrWhiteSpace(dto.CurrentLocation)) notesBuilder.Append($", Location={dto.CurrentLocation}");
            if (!string.IsNullOrWhiteSpace(dto.Notes)) notesBuilder.Append($". Note: {dto.Notes}");

            var history = new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = dto.Status,
                Notes = notesBuilder.ToString(),
                ChangedBy = changedBy,
                CreatedAt = DateTime.UtcNow
            };

            await _context.OrderStatusHistories.AddAsync(history);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePaymentStatusAsync(int orderId, PaymentStatus newStatus, string? notes, string changedBy)
        {
            var order = await _context.Orders.Include(o => o.Payments).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return;

            order.PaymentStatus = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            var payment = order.Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
            if (payment != null)
            {
                payment.Status = newStatus;
                if (newStatus == PaymentStatus.Paid && payment.PaidAt == null)
                {
                    payment.PaidAt = DateTime.UtcNow;
                }
            }

            var history = new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = order.Status,
                Notes = $"Payment status updated to {newStatus}" + (!string.IsNullOrWhiteSpace(notes) ? $": {notes}" : ""),
                ChangedBy = changedBy,
                CreatedAt = DateTime.UtcNow
            };

            await _context.OrderStatusHistories.AddAsync(history);
            await _context.SaveChangesAsync();
        }

        public async Task CancelOrderAsync(int orderId, string userId, string reason)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null || order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Cancelled)
            {
                throw new InvalidOperationException("This order cannot be cancelled in its current status.");
            }

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            // Restock items
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                    if (item.ProductVariantId.HasValue && item.ProductVariantId.Value > 0)
                    {
                        var variant = await _context.ProductVariants.FindAsync(item.ProductVariantId.Value);
                        if (variant != null)
                        {
                            variant.StockQuantity += item.Quantity;
                        }
                    }

                    var tx = new StockTransaction
                    {
                        ProductId = item.ProductId,
                        ProductVariantId = item.ProductVariantId,
                        QuantityChange = item.Quantity,
                        RemainingStock = product.StockQuantity,
                        ChangeType = StockChangeType.Return,
                        Reference = $"Order {order.OrderNumber} Cancellation",
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.StockTransactions.AddAsync(tx);
                }
            }

            var history = new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = OrderStatus.Cancelled,
                Notes = $"Cancelled by Customer: {reason}",
                ChangedBy = "Customer",
                CreatedAt = DateTime.UtcNow
            };
            await _context.OrderStatusHistories.AddAsync(history);
            await _context.SaveChangesAsync();
        }

        public async Task RequestReturnAsync(ReturnRequestDto dto, string userId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.UserId == userId);

            if (order == null)
            {
                throw new InvalidOperationException("Order not found.");
            }

            if (order.Status != OrderStatus.Delivered)
            {
                throw new InvalidOperationException("Only delivered orders are eligible for return.");
            }

            if ((DateTime.UtcNow - order.OrderDate).TotalDays > 7)
            {
                throw new InvalidOperationException("The 7-day return window for this order has expired.");
            }

            order.Status = OrderStatus.ReturnRequested;
            order.ReturnReason = dto.Reason;
            order.ReturnComments = dto.Comments;
            order.ReturnRefundPaymentDetails = dto.RefundPaymentDetails;
            order.ReturnRequestedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            var history = new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = OrderStatus.ReturnRequested,
                Notes = $"Return Requested: {dto.Reason}. Details: {dto.Comments}",
                ChangedBy = "Customer",
                CreatedAt = DateTime.UtcNow
            };
            await _context.OrderStatusHistories.AddAsync(history);

            await _context.SaveChangesAsync();

            // Notify Admin & Customer via Email
            try
            {
                await _emailService.SendEmailAsync(
                    order.User?.Email ?? "customer@store.com",
                    $"Return Request Received - Order #{order.OrderNumber}",
                    $@"<h3>Hello {order.ShippingFullName},</h3>
                       <p>We have received your return request for Order <strong>#{order.OrderNumber}</strong>.</p>
                       <p><strong>Reason:</strong> {dto.Reason}</p>
                       <p><strong>Details:</strong> {dto.Comments}</p>
                       <p>Our support team is reviewing your request and will update you shortly.</p>"
                );
            }
            catch { /* non-blocking */ }
        }

        public async Task ProcessReturnAsync(ProcessReturnDto dto, string adminUser)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

            if (order == null)
            {
                throw new InvalidOperationException("Order not found.");
            }

            string action = dto.Action?.Trim() ?? "Approve";
            order.ReturnActionAt = DateTime.UtcNow;
            order.RefundAdminNotes = dto.AdminNotes;
            order.UpdatedAt = DateTime.UtcNow;

            if (action.Equals("Approve", StringComparison.OrdinalIgnoreCase))
            {
                order.Status = OrderStatus.Returned;
                order.RefundAmount = dto.RefundAmount ?? order.GrandTotal;

                var history = new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = OrderStatus.Returned,
                    Notes = $"Return Approved by Admin: {dto.AdminNotes}",
                    ChangedBy = adminUser,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.OrderStatusHistories.AddAsync(history);

                if (dto.RestockInventory)
                {
                    await RestockOrderItemsAsync(order, $"Order {order.OrderNumber} Return Restock");
                }
            }
            else if (action.Equals("Refund", StringComparison.OrdinalIgnoreCase))
            {
                order.Status = OrderStatus.Refunded;
                order.PaymentStatus = PaymentStatus.Refunded;
                order.RefundAmount = dto.RefundAmount ?? order.GrandTotal;
                order.RefundReferenceId = dto.RefundReferenceId ?? $"REF-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

                var history = new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = OrderStatus.Refunded,
                    Notes = $"Refund of ₹{order.RefundAmount:N2} Issued (Ref: {order.RefundReferenceId}). Notes: {dto.AdminNotes}",
                    ChangedBy = adminUser,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.OrderStatusHistories.AddAsync(history);

                if (dto.RestockInventory)
                {
                    await RestockOrderItemsAsync(order, $"Order {order.OrderNumber} Refund Restock");
                }
            }
            else if (action.Equals("Reject", StringComparison.OrdinalIgnoreCase))
            {
                order.Status = OrderStatus.Delivered; // Return back to Delivered state
                var history = new OrderStatusHistory
                {
                    OrderId = order.Id,
                    Status = OrderStatus.Delivered,
                    Notes = $"Return Request Rejected by Admin: {dto.AdminNotes}",
                    ChangedBy = adminUser,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.OrderStatusHistories.AddAsync(history);
            }

            await _context.SaveChangesAsync();
        }

        private async Task RestockOrderItemsAsync(Order order, string reference)
        {
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                    if (item.ProductVariantId.HasValue && item.ProductVariantId.Value > 0)
                    {
                        var variant = await _context.ProductVariants.FindAsync(item.ProductVariantId.Value);
                        if (variant != null)
                        {
                            variant.StockQuantity += item.Quantity;
                        }
                    }

                    var tx = new StockTransaction
                    {
                        ProductId = item.ProductId,
                        ProductVariantId = item.ProductVariantId,
                        QuantityChange = item.Quantity,
                        RemainingStock = product.StockQuantity,
                        ChangeType = StockChangeType.Return,
                        Reference = reference,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _context.StockTransactions.AddAsync(tx);
                }
            }
        }
    }
}
