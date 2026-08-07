using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MTKPM_Clothing_Store_web.Models;
using MTKPM_Clothing_Store_web.Services;
using System.Threading.Tasks;

namespace MTKPM_Clothing_Store_web.Controllers
{
    // Trang Chăm sóc khách hàng - FAQ tĩnh, không cần database.
    [AllowAnonymous]
    public class SupportController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<SupportController> _logger;

        public SupportController(IEmailService emailService, IOptions<SmtpSettings> smtpOptions, ILogger<SupportController> logger)
        {
            _emailService = emailService;
            _smtpSettings = smtpOptions.Value;
            _logger = logger;
        }

        // GET: /Support/Faq
        public IActionResult Faq()
        {
            return View();
        }

        // GET: /Support/Chat
        public IActionResult Chat()
        {
            var model = new SupportMessageViewModel
            {
                Name = User?.Identity?.IsAuthenticated == true ? User.Identity?.Name : null,
                Email = User?.Identity?.IsAuthenticated == true ? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value : null
            };
            return View(model);
        }

        // POST: /Support/Chat
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Chat([FromForm] SupportMessageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // If AJAX, return validation errors as JSON
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(ModelState);
                }
                return View(model);
            }

            try
            {
                var to = _smtpSettings.FromEmail; // send to store support email (configured)
                var subject = $"Customer message from {model.Name ?? "Guest"}";
                if (!string.IsNullOrWhiteSpace(model.OrderNumber))
                {
                    subject += $" (Order: {model.OrderNumber})";
                }

                var html = $@"
<p><strong>Name:</strong> {System.Net.WebUtility.HtmlEncode(model.Name)}</p>
<p><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(model.Email)}</p>
<p><strong>Order:</strong> {System.Net.WebUtility.HtmlEncode(model.OrderNumber)}</p>
<hr />
<p>{System.Net.WebUtility.HtmlEncode(model.Message).Replace("\n", "<br/>")}</p>
";

                await _emailService.SendEmailAsync(to, subject, html);

                _logger.LogInformation("Support message sent from {Email}", model.Email);

                // If AJAX request, return JSON for client script
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Message sent. Our support team will contact you by email." });
                }

                TempData["SupportMessageSent"] = "Message sent. Our support team will contact you by email.";
                return RedirectToAction(nameof(Chat));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send support message");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return StatusCode(500, new { success = false, message = "Unable to send message right now." });
                }
                ModelState.AddModelError("", "Unable to send message right now.");
                return View(model);
            }
        }
    }
}