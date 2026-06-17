using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace MTKPM_Clothing_Store_web.Controllers
{
    public class UsersController : Controller
    {
        private readonly ClothingStoreContext _context;

        public UsersController(ClothingStoreContext context)
        {
            _context = context;
        }

        // GET: Users/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Users/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("UserId,Name,Email,Password,Role")] User user)
        {
            if (string.IsNullOrWhiteSpace(user?.Email) || string.IsNullOrWhiteSpace(user?.Password) || string.IsNullOrWhiteSpace(user?.Name))
            {
                ModelState.AddModelError(string.Empty, "Name, Email và Password là bắt buộc.");
                return View(user);
            }

            // Kiểm tra email đã tồn tại
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ModelState.AddModelError(nameof(user.Email), "Email này đã được đăng ký.");
                return View(user);
            }

            if (ModelState.IsValid)
            {
                // Đặt role mặc định nếu chưa có
                if (string.IsNullOrWhiteSpace(user.Role))
                {
                    user.Role = "User";
                }

                // Băm mật khẩu trước khi lưu
                var hasher = new PasswordHasher<User>();
                var plain = user.Password;
                user.Password = hasher.HashPassword(user, plain);

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Login));
            }
            return View(user);
        }

        // GET: Users/Login
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Users/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required.");
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            var hasher = new PasswordHasher<User>();
            var verifyResult = hasher.VerifyHashedPassword(user, user.Password, password);

            // Nếu mật khẩu đã băm và khớp -> OK
            if (verifyResult == PasswordVerificationResult.Success || verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                // Nếu cần rehash theo thuật toán mới, PasswordHasher trả SuccessRehashNeeded
                if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.Password = hasher.HashPassword(user, password);
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // Fallback chuyển tiếp: nếu DB đang chứa plain-text (tồn tại trước khi băm)
                if (user.Password == password)
                {
                    // Băm lại mật khẩu một lần và lưu
                    user.Password = hasher.HashPassword(user, password);
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    ViewData["ReturnUrl"] = returnUrl;
                    return View();
                }
            }

            // Tạo claims và sign-in
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Name", user.Name ?? string.Empty);
            HttpContext.Session.SetString("Role", user.Role ?? string.Empty);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Home");

            return RedirectToAction("Index", "Home");
        }

        // POST: Users/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Sign out the cookie authentication scheme
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear session values
            HttpContext.Session.Clear();

            // Prevent cached pages being shown after logout
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction("Login");
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,Name,Email,Password,Role")] User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,Name,Email,Role")] User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            // Remove validation for Password (form không gửi Password)
            ModelState.Remove("Password");
            ModelState.Remove("user.Password");
            ModelState.Remove("User.Password");

            // Server-side basic validation
            if (string.IsNullOrWhiteSpace(user.Name))
            {
                ModelState.AddModelError(nameof(user.Name), "Tên là bắt buộc.");
            }
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError(nameof(user.Email), "Email là bắt buộc.");
            }

            // Check email uniqueness (exclude current user)
            if (await _context.Users.AnyAsync(u => u.Email == user.Email && u.UserId != id))
            {
                ModelState.AddModelError(nameof(user.Email), "Email này đã được sử dụng bởi tài khoản khác.");
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            // Only update allowed fields (do not touch Password here)
            existingUser.Name = user.Name;
            existingUser.Email = user.Email;
            existingUser.Role = user.Role;

            try
            {
                _context.Update(existingUser);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.UserId == user.UserId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
