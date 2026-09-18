using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ecommerce.Application.Interfaces;

namespace Ecommerce.Infrastructure.Services
{
    /// <summary>
    /// Email Notification Service:
    /// Gmail SMTP server ke zariye Admin ko Customer Inquiry aur Order notifications bhejne ka kaam karta hai.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Generic HTML Email Dispatcher:
        /// appsettings.json ke EmailSettings block se SMTP Host, Port, Email aur Password padhkar email bhejta hai.
        /// </summary>
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? fromName = null)
        {
            try
            {
                // 1. appsettings.json se SMTP configuration read karna
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPortStr = _configuration["EmailSettings:SmtpPort"];
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

                // Agar SMTP configured na ho toh safe fallback ke taur par mock log print karta hai
                if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(senderEmail))
                {
                    _logger.LogInformation("[EmailService] SMTP not configured. Mock dispatching email to {ToEmail} with Subject: {Subject}", toEmail, subject);
                    return;
                }

                int smtpPort = int.TryParse(smtpPortStr, out int p) ? p : 587;

                // 2. Google App Password se extra spaces remove karna
                var cleanPassword = senderPassword?.Trim().Replace(" ", "") ?? string.Empty;

                // 3. Email Message Prepare karna (From, To, Subject, Body)
                using var message = new MailMessage
                {
                    From = new MailAddress(senderEmail, fromName ?? "Cartivo Support"),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(toEmail);

                // 4. SMTP Client initialize karke SSL connection banana
                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = enableSsl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail, cleanPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                // 5. Asynchronous Email send karna
                await client.SendMailAsync(message);
                _logger.LogInformation("[EmailService] Email successfully sent to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                // Error catch karna taaki user ki screen par crash na aaye
                _logger.LogError(ex, "[EmailService] Failed to send email to {ToEmail}", toEmail);
            }
        }

        /// <summary>
        /// Customer Contact Form Notification:
        /// Jab koi user Contact Us page se message bhejta hai, toh sundar HTML template banakar Admin ko email bhejta hai.
        /// </summary>
        public async Task SendContactInquiryNotificationAsync(string name, string email, string? phone, string subject, string message)
        {
            var adminEmail = _configuration["EmailSettings:AdminNotificationEmail"] ?? "anandmishra02.com@gmail.com";

            // Responsive & Modern HTML Email Design
            var emailHtml = $@"
                <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 25px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;"">
                    <div style=""border-bottom: 2px solid #3b82f6; padding-bottom: 15px; margin-bottom: 20px;"">
                        <h2 style=""color: #1e3a8a; margin: 0; font-size: 22px;"">📬 New Customer Inquiry / Complaint</h2>
                        <span style=""color: #64748b; font-size: 13px;"">Received on {DateTime.UtcNow:MMMM dd, yyyy hh:mm tt} UTC</span>
                    </div>

                    <div style=""margin-bottom: 20px; background-color: #f8fafc; padding: 15px; border-radius: 8px; border-left: 4px solid #3b82f6;"">
                        <p style=""margin: 6px 0;""><strong>Customer Name:</strong> {name}</p>
                        <p style=""margin: 6px 0;""><strong>Email Address:</strong> <a href=""mailto:{email}"" style=""color: #2563eb;"">{email}</a></p>
                        {(string.IsNullOrWhiteSpace(phone) ? "" : $"<p style=\"margin: 6px 0;\"><strong>Phone Number:</strong> {phone}</p>")}
                        <p style=""margin: 6px 0;""><strong>Subject:</strong> {subject}</p>
                    </div>

                    <div style=""margin-bottom: 25px;"">
                        <h4 style=""color: #334155; margin-bottom: 8px;"">Message Content:</h4>
                        <div style=""background-color: #ffffff; padding: 15px; border: 1px solid #cbd5e1; border-radius: 8px; font-size: 15px; line-height: 1.6; color: #1e293b;"">
                            {message.Replace("\n", "<br/>")}
                        </div>
                    </div>

                    <div style=""border-top: 1px solid #e2e8f0; padding-top: 15px; text-align: center;"">
                        <a href=""mailto:{email}?subject=Re: {Uri.EscapeDataString(subject)}"" style=""display: inline-block; background-color: #2563eb; color: #ffffff; text-decoration: none; padding: 10px 24px; border-radius: 25px; font-weight: bold; font-size: 14px;"">
                            ✉️ Reply to Customer
                        </a>
                    </div>
                </div>";

            await SendEmailAsync(adminEmail, $"[New Inquiry] {subject} - From {name}", emailHtml, "Cartivo Customer Support");
        }
    }
}
