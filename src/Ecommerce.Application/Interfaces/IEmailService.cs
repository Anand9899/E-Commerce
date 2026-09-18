using System.Threading.Tasks;

namespace Ecommerce.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? fromName = null);
        Task SendContactInquiryNotificationAsync(string name, string email, string? phone, string subject, string message);
    }
}
