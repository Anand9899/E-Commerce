using System.Collections.Generic;
using System.Threading.Tasks;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces
{
    public interface IContactService
    {
        Task<int> SubmitContactMessageAsync(ContactFormVM model);
        Task<List<ContactMessageListVM>> GetAllMessagesAsync(bool? unreadOnly = null);
        Task<ContactMessageDetailVM?> GetMessageByIdAsync(int id, bool markAsRead = true);
        Task<int> GetUnreadCountAsync();
        Task MarkAsReadAsync(int id);
        Task DeleteMessageAsync(int id);
    }
}
