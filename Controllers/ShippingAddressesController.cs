using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Models;
using System.Security.Claims;

namespace MTKPM_Clothing_Store_web.Controllers
{
    // Sổ địa chỉ giao hàng - chỉ dành cho user đã đăng nhập (khách vãng lai không có sổ địa chỉ,
    // họ nhập địa chỉ trực tiếp lúc thanh toán).
    [Authorize]
    public class ShippingAddressesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ShippingAddressesController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private int GetCurrentUserId()
        {
            var userIdString = _httpContextAccessor.HttpContext?.Session.GetString("UserId")
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdString, out var id) ? id : 0;
        }

        // GET: ShippingAddresses
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var addresses = await _context.ShippingAddresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(addresses);
        }

        // GET: ShippingAddresses/Create
        public IActionResult Create()
        {
            return View(new ShippingAddress());
        }

        // POST: ShippingAddresses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReceiverName,PhoneNumber,Province,Ward,Street,AddressLabel,IsDefault")] ShippingAddress model)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Forbid();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.UserId = userId;
            model.CreatedAt = DateTime.UtcNow;

            // Nếu đây là địa chỉ đầu tiên của user, tự động đặt làm mặc định.
            bool hasAnyAddress = await _context.ShippingAddresses.AnyAsync(a => a.UserId == userId);
            if (!hasAnyAddress) model.IsDefault = true;

            if (model.IsDefault)
            {
                // Bỏ mặc định ở các địa chỉ khác trước khi thêm địa chỉ mặc định mới.
                var others = await _context.ShippingAddresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync();
                foreach (var o in others) o.IsDefault = false;
            }

            _context.ShippingAddresses.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm địa chỉ mới.";
            return RedirectToAction(nameof(Index));
        }

        // GET: ShippingAddresses/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            var address = await _context.ShippingAddresses.FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == userId);
            if (address == null) return NotFound();

            return View(address);
        }

        // POST: ShippingAddresses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AddressId,ReceiverName,PhoneNumber,Province,Ward,Street,AddressLabel,IsDefault")] ShippingAddress posted)
        {
            if (id != posted.AddressId) return BadRequest();

            var userId = GetCurrentUserId();
            var address = await _context.ShippingAddresses.FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == userId);
            if (address == null) return NotFound();

            if (!ModelState.IsValid)
            {
                posted.UserId = userId;
                return View(posted);
            }

            address.ReceiverName = posted.ReceiverName;
            address.PhoneNumber = posted.PhoneNumber;
            address.Province = posted.Province;
            address.Ward = posted.Ward;
            address.Street = posted.Street;
            address.AddressLabel = posted.AddressLabel;

            if (posted.IsDefault && !address.IsDefault)
            {
                var others = await _context.ShippingAddresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync();
                foreach (var o in others) o.IsDefault = false;
                address.IsDefault = true;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật địa chỉ.";
            return RedirectToAction(nameof(Index));
        }

        // POST: ShippingAddresses/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            var address = await _context.ShippingAddresses.FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == userId);
            if (address == null) return NotFound();

            bool wasDefault = address.IsDefault;
            _context.ShippingAddresses.Remove(address);
            await _context.SaveChangesAsync();

            // Nếu vừa xóa địa chỉ mặc định, tự động đặt 1 địa chỉ còn lại (nếu có) làm mặc định mới.
            if (wasDefault)
            {
                var next = await _context.ShippingAddresses.Where(a => a.UserId == userId).FirstOrDefaultAsync();
                if (next != null)
                {
                    next.IsDefault = true;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["Success"] = "Đã xóa địa chỉ.";
            return RedirectToAction(nameof(Index));
        }

        // POST: ShippingAddresses/SetDefault/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefault(int id)
        {
            var userId = GetCurrentUserId();
            var addresses = await _context.ShippingAddresses.Where(a => a.UserId == userId).ToListAsync();
            var target = addresses.FirstOrDefault(a => a.AddressId == id);
            if (target == null) return NotFound();

            foreach (var a in addresses) a.IsDefault = (a.AddressId == id);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã đặt làm địa chỉ mặc định.";
            return RedirectToAction(nameof(Index));
        }
    }
}