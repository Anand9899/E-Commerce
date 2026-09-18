using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;

namespace Ecommerce.Infrastructure.Services
{
    /// <summary>
    /// Contact & Inquiries Service:
    /// Users dwara bheje gaye inquiries/complaints ko database me save karne,
    /// Admin ko email notification bhejne aur Admin Panel me messages manage karne ka kaam karta hai.
    /// </summary>
    public class ContactService : IContactService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public ContactService(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        /// <summary>
        /// Contact Form se aayi inquiry ko database me insert karta hai
        /// aur background me Admin ko email notification dispatch karta hai.
        /// </summary>
        public async Task<int> SubmitContactMessageAsync(ContactFormVM model)
        {
            var message = new ContactMessage
            {
                Name = model.Name.Trim(),
                Email = model.Email.Trim().ToLowerInvariant(),
                Phone = model.Phone?.Trim(),
                Subject = model.Subject.Trim(),
                Message = model.Message.Trim(),
                IsRead = false,
                IsReplied = false,
                CreatedAt = DateTime.UtcNow
            };

            // 1. Database me inquiry message save karna
            await _context.ContactMessages.AddAsync(message);
            await _context.SaveChangesAsync();

            // 2. Background Task me Admin ko email notification bhejna (Taaki user ka web page bina wait kiye turant respond kare)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendContactInquiryNotificationAsync(
                        message.Name,
                        message.Email,
                        message.Phone,
                        message.Subject,
                        message.Message
                    );
                }
                catch
                {
                    // Error logging is already handled inside EmailService
                }
            });

            return message.Id;
        }

        /// <summary>
        /// Admin Panel ke liye sabhi inquiries / complaints ki list fetch karta hai.
        /// </summary>
        public async Task<List<ContactMessageListVM>> GetAllMessagesAsync(bool? unreadOnly = null)
        {
            var query = _context.ContactMessages.AsNoTracking().AsQueryable();

            if (unreadOnly.HasValue && unreadOnly.Value)
            {
                query = query.Where(m => !m.IsRead);
            }

            return await query
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new ContactMessageListVM
                {
                    Id = m.Id,
                    Name = m.Name,
                    Email = m.Email,
                    Phone = m.Phone,
                    Subject = m.Subject,
                    MessageSnippet = m.Message.Length > 80 ? m.Message.Substring(0, 80) + "..." : m.Message,
                    IsRead = m.IsRead,
                    IsReplied = m.IsReplied,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();
        }

        /// <summary>
        /// Message ki detail dekhne aur use automatically "Read" mark karne ke liye.
        /// </summary>
        public async Task<ContactMessageDetailVM?> GetMessageByIdAsync(int id, bool markAsRead = true)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return null;

            if (markAsRead && !message.IsRead)
            {
                message.IsRead = true;
                message.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return new ContactMessageDetailVM
            {
                Id = message.Id,
                Name = message.Name,
                Email = message.Email,
                Phone = message.Phone,
                Subject = message.Subject,
                Message = message.Message,
                IsRead = message.IsRead,
                IsReplied = message.IsReplied,
                AdminNotes = message.AdminNotes,
                RepliedBy = message.RepliedBy,
                RepliedAt = message.RepliedAt,
                CreatedAt = message.CreatedAt
            };
        }

        /// <summary>
        /// Kitne messages unread (naye) hain unka count return karta hai.
        /// </summary>
        public async Task<int> GetUnreadCountAsync()
        {
            return await _context.ContactMessages.CountAsync(m => !m.IsRead);
        }

        /// <summary>
        /// Message ko mark as read karta hai.
        /// </summary>
        public async Task MarkAsReadAsync(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message != null && !message.IsRead)
            {
                message.IsRead = true;
                message.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Message ko soft-delete (archive) karta hai.
        /// </summary>
        public async Task DeleteMessageAsync(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message != null)
            {
                message.IsDeleted = true;
                message.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
