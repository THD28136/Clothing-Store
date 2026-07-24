using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MTKPM_Clothing_Store_web.Controllers
{
    // Trang Chăm sóc khách hàng - FAQ tĩnh, không cần database.
    [AllowAnonymous]
    public class SupportController : Controller
    {
        // GET: /Support/Faq
        public IActionResult Faq()
        {
            return View();
        }
    }
}