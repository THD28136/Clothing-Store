using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Models;
using System.Security.Claims;

namespace MTKPM_Clothing_Store_web.Controllers
{
    // Đánh giá / phản hồi sản phẩm.
    // Bắt buộc đăng nhập vì bảng reviews yêu cầu user_id (không hỗ trợ khách vãng lai).
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReviewsController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private int? GetCurrentUserId()
        {
            var userIdString = _httpContextAccessor.HttpContext?.Session.GetString("UserId")
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdString, out var id) ? id : (int?)null;
        }

        // POST: Reviews/Create
        // Mỗi user chỉ có 1 đánh giá / sản phẩm: gửi lại lần 2 sẽ cập nhật đánh giá cũ.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productId, int rating, string? comment)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Users", new { returnUrl = Url.Action("Details", "Products", new { id = productId }) });
            }

            var productExists = await _context.Products.AnyAsync(p => p.ProductId == productId);
            if (!productExists) return NotFound();

            // Chỉ cho phép đánh giá nếu user đã từng đặt sản phẩm này (đơn không bị hủy)
            bool hasPurchased = await _context.Orders
                .Where(o => o.UserId == userId.Value && o.Status != "Cancelled")
                .AnyAsync(o => o.OrderDetails.Any(od => od.ProductId == productId));

            if (!hasPurchased)
            {
                TempData["ReviewError"] = "Bạn cần đặt mua sản phẩm này trước khi có thể đánh giá.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            if (rating < 1 || rating > 5)
            {
                TempData["ReviewError"] = "Vui lòng chọn số sao đánh giá từ 1 đến 5.";
                return RedirectToAction("Details", "Products", new { id = productId });
            }

            comment = comment?.Trim();
            if (!string.IsNullOrEmpty(comment) && comment.Length > 1000)
            {
                comment = comment.Substring(0, 1000);
            }

            var existing = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId.Value);

            if (existing != null)
            {
                existing.Rating = rating;
                existing.Comment = comment;
                existing.ReviewDate = DateTime.UtcNow;
                _context.Update(existing);
                TempData["ReviewSuccess"] = "Đánh giá của bạn đã được cập nhật.";
            }
            else
            {
                var review = new Review
                {
                    ProductId = productId,
                    UserId = userId.Value,
                    Rating = rating,
                    Comment = comment,
                    ReviewDate = DateTime.UtcNow
                };
                _context.Reviews.Add(review);
                TempData["ReviewSuccess"] = "Cảm ơn bạn đã gửi đánh giá!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Products", new { id = productId });
        }

        // POST: Reviews/Delete/5
        // Cho phép: chủ đánh giá hoặc Admin.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && (userId == null || review.UserId != userId.Value))
            {
                return Forbid();
            }

            var productId = review.ProductId;
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] = "Đã xóa đánh giá.";
            return RedirectToAction("Details", "Products", new { id = productId });
        }
    }
}