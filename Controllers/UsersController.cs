using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;

namespace MTKPM_Clothing_Store_web.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Users
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        // GET: Users/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // GET: Users/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("UserId,Name,Email,Password,Username,Phone,Address,Role")] User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5 (admin)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();
            return View(user);
        }

        // POST: Users/Edit/5 (admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,Name,Email,Username,Phone,Address,Role")] User user)
        {
            if (id != user.UserId)
                return NotFound();

            ModelState.Remove("Password"); // Don't update password here

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Users/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user != null)
                {
                    var hasher = new PasswordHasher<User>();
                    var result = hasher.VerifyHashedPassword(user, user.Password, password);

                    if (result == PasswordVerificationResult.Success)
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                            new Claim(ClaimTypes.Name, user.Name),
                            new Claim(ClaimTypes.Email, user.Email),
                            new Claim(ClaimTypes.Role, user.Role)
                        };

                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                        return Redirect(returnUrl ?? "/");
                    }
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View();
        }

        // GET: Users/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Users/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register([Bind("UserId,Name,Email,Password,Role")] User user)
        {
            if (ModelState.IsValid)
            {
                var hasher = new PasswordHasher<User>();
                user.Password = hasher.HashPassword(user, user.Password);

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Login));
            }
            return View(user);
        }

        // GET: Users/EditProfile/5 - allow user to edit their own profile
        [Authorize]
        public async Task<IActionResult> EditProfile(int? id)
        {
            if (id == null) return NotFound();

            // only allow admin or the user themselves
            var currentIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentIdStr, out var currentId))
                return Forbid();

            if (!User.IsInRole("Admin") && currentId != id.Value)
                return Forbid();

            var user = await _context.Users.FindAsync(id.Value);
            if (user == null) return NotFound();

            return View("EditProfile", user);
        }

        // POST: Users/EditProfile/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EditProfile(int id, [Bind("UserId,Name,Email,Username,Phone,Address")] User posted)
        {
            if (id != posted.UserId) return BadRequest();

            // ownership check
            var currentIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentIdStr, out var currentId))
                return Forbid();

            if (!User.IsInRole("Admin") && currentId != id)
                return Forbid();

            ModelState.Remove("Password"); // Do not require password for profile update

            // check email uniqueness
            var exists = await _context.Users.AnyAsync(u => u.Email == posted.Email && u.UserId != id);
            if (exists)
            {
                ModelState.AddModelError(nameof(posted.Email), "Email đã được sử dụng bởi người khác.");
            }

            if (!ModelState.IsValid)
            {
                return View("EditProfile", posted);
            }

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) return NotFound();

                user.Name = posted.Name;
                user.Email = posted.Email;
                user.Username = posted.Username;
                user.Phone = posted.Phone;
                user.Address = posted.Address;

                _context.Update(user);
                await _context.SaveChangesAsync();

                // If user updated their name/email, update claims cookie (optional)
                if (currentId == id)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
                        new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                        new Claim(ClaimTypes.Role, user.Role ?? string.Empty)
                    };
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
                }

                return RedirectToAction(nameof(Details), new { id = id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(posted.UserId)) return NotFound();
                throw;
            }
        }

        // POST: Users/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            // Sign out the cookie authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear session to remove any user-related data (cart, user id, etc.)
            HttpContext.Session.Clear();

            // Redirect to login page (or home)
            return RedirectToAction(nameof(Login));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
