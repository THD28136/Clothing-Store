using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MTKPM_Clothing_Store_web.Services
{
    // Gửi email qua SMTP (vd: Gmail SMTP với App Password).
    // Không throw ra ngoài khi gửi thất bại — chỉ log lỗi, để việc gửi email
    // không bao giờ làm hỏng luồng đặt hàng của khách.
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("SendEmailAsync called with empty recipient; skipping.");
                return;
            }

            try
            {
                using var client = new SmtpClient(_settings.Host, _settings.Port)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.User, _settings.Password)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.FromEmail, _settings.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(toEmail);

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent to {ToEmail} with subject '{Subject}'.", toEmail, subject);
            }
            catch (Exception ex)
            {
                // Không để lỗi gửi mail làm hỏng luồng đặt hàng — chỉ log lại.
                _logger.LogError(ex, "Failed to send email to {ToEmail}.", toEmail);
            }
        }
    }
}