namespace Ecommerce.Domain.Enums
{
    public enum OrderStatus
    {
        Pending = 0,
        Confirmed = 1,
        Processing = 2,
        Shipped = 3,
        OutForDelivery = 4,
        Delivered = 5,
        Cancelled = 6,
        ReturnRequested = 7,
        Returned = 8,
        Refunded = 9
    }
}
