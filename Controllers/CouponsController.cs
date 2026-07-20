using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Models;

namespace MTKPM_Clothing_Store_web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CouponsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CouponsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Coupons
        public async Task<IActionResult> Index()
        {
            var coupons = await _context.Coupons
                .OrderByDescending(c => c.CouponId)
                .ToListAsync();

            return View(coupons);
        }

        // GET: Coupons/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Coupons/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CouponName,Code,DiscountPercent,ExpiryDate,UsageLimit,MinOrderAmount")] Coupon coupon)
        {
            if (string.IsNullOrWhiteSpace(coupon.Code))
            {
                ModelState.AddModelError(nameof(coupon.Code), "Mã giảm giá là bắt buộc.");
            }
            else
            {
                coupon.Code = coupon.Code.Trim();
                var exists = await _context.Coupons.AnyAsync(c => c.Code == coupon.Code);
                if (exists)
                {
                    ModelState.AddModelError(nameof(coupon.Code), "Mã giảm giá này đã tồn tại.");
                }
            }

            if (coupon.DiscountPercent is null or <= 0 or > 100)
            {
                ModelState.AddModelError(nameof(coupon.DiscountPercent), "Phần trăm giảm giá phải trong khoảng 1-100.");
            }

            if (ModelState.IsValid)
            {
                coupon.UsedCount = 0;
                _context.Add(coupon);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(coupon);
        }

        // GET: Coupons/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            return View(coupon);
        }

        // POST: Coupons/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CouponId,CouponName,Code,DiscountPercent,ExpiryDate,UsageLimit,UsedCount,MinOrderAmount")] Coupon coupon)
        {
            if (id != coupon.CouponId) return NotFound();

            if (string.IsNullOrWhiteSpace(coupon.Code))
            {
                ModelState.AddModelError(nameof(coupon.Code), "Mã giảm giá là bắt buộc.");
            }
            else
            {
                coupon.Code = coupon.Code.Trim();
                var codeTaken = await _context.Coupons.AnyAsync(c => c.Code == coupon.Code && c.CouponId != id);
                if (codeTaken)
                {
                    ModelState.AddModelError(nameof(coupon.Code), "Mã giảm giá này đã tồn tại.");
                }
            }

            if (coupon.DiscountPercent is null or <= 0 or > 100)
            {
                ModelState.AddModelError(nameof(coupon.DiscountPercent), "Phần trăm giảm giá phải trong khoảng 1-100.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(coupon);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CouponExists(coupon.CouponId)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(coupon);
        }

        // GET: Coupons/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.CouponId == id);
            if (coupon == null) return NotFound();

            return View(coupon);
        }

        // POST: Coupons/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CouponExists(int id)
        {
            return _context.Coupons.Any(e => e.CouponId == id);
        }
    }
}