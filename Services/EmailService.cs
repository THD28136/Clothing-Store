using Microsoft.Extensions.Options;
using MTKPM_Clothing_Store_web.Models;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MTKPM_Clothing_Store_web.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(IOptions<SmtpSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
            message.To.Add(new MailAddress(to));
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.User, _settings.Password)
            };

            await client.SendMailAsync(message);
        }
    }
}