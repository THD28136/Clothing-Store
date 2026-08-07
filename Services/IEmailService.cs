using System.Threading.Tasks;

namespace MTKPM_Clothing_Store_web.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody);
    }
}