using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Models;

namespace MTKPM_Clothing_Store_web.Services
{
    public class LoyaltyService : ILoyaltyService
    {
        private readonly ApplicationDbContext _context;

        // Ngưỡng hạng thành viên (tổng tiền các đơn Delivered) + % giảm giá tự động tương ứng.
        // Chỉnh sửa ở đây nếu muốn đổi mức hạng.
        private static readonly (decimal Threshold, string Name, decimal DiscountPercent)[] Tiers = new[]
        {
            (0m, "Thân thiết", 0m),
            (3_000_000m, "Bạc", 2m),
            (10_000_000m, "Vàng", 5m),
            (30_000_000m, "Kim cương", 10m),
        };

        // Quy đổi: mỗi 10.000đ chi tiêu (đơn Delivered) = 1 điểm.
        private const decimal VndPerPoint = 10_000m;

        public LoyaltyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetTotalSpentAsync(int userId)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId && o.Status == "Delivered")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
        }

        public async Task<LoyaltyTierInfo> GetTierInfoAsync(int userId)
        {
            var totalSpent = await GetTotalSpentAsync(userId);

            var currentTierIndex = 0;
            for (int i = 0; i < Tiers.Length; i++)
            {
                if (totalSpent >= Tiers[i].Threshold) currentTierIndex = i;
            }

            var current = Tiers[currentTierIndex];
            var next = currentTierIndex + 1 < Tiers.Length ? Tiers[currentTierIndex + 1] : ((decimal, string, decimal)?)null;

            return new LoyaltyTierInfo
            {
                TierName = current.Name,
                DiscountPercent = current.DiscountPercent,
                TotalSpent = totalSpent,
                NextTierThreshold = next?.Item1,
                NextTierName = next?.Item2
            };
        }

        public async Task<int> GetAvailablePointsAsync(int userId)
        {
            return await _context.LoyaltyPointTransactions
                .Where(t => t.UserId == userId)
                .SumAsync(t => (int?)t.Points) ?? 0;
        }

        public async Task<bool> EarnPointsForOrderAsync(int userId, int orderId, decimal orderTotal)
        {
            // Chống cộng trùng: nếu đã có giao dịch điểm dương gắn với đơn này thì bỏ qua.
            var alreadyEarned = await _context.LoyaltyPointTransactions
                .AnyAsync(t => t.OrderId == orderId && t.Points > 0);
            if (alreadyEarned) return false;

            var points = (int)Math.Floor(orderTotal / VndPerPoint);
            if (points <= 0) return false;

            _context.LoyaltyPointTransactions.Add(new LoyaltyPointTransaction
            {
                UserId = userId,
                OrderId = orderId,
                Points = points,
                Description = $"Tích điểm từ đơn hàng #{orderId}",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReversePointsForOrderAsync(int orderId)
        {
            var earnTx = await _context.LoyaltyPointTransactions
                .FirstOrDefaultAsync(t => t.OrderId == orderId && t.Points > 0);
            if (earnTx == null) return false;

            // Đã có giao dịch trừ bù cho đơn này rồi thì không trừ thêm lần nữa.
            var alreadyReversed = await _context.LoyaltyPointTransactions
                .AnyAsync(t => t.OrderId == orderId && t.Points < 0 && t.Description == $"Hoàn điểm do hủy đơn hàng #{orderId}");
            if (alreadyReversed) return false;

            _context.LoyaltyPointTransactions.Add(new LoyaltyPointTransaction
            {
                UserId = earnTx.UserId,
                OrderId = orderId,
                Points = -earnTx.Points,
                Description = $"Hoàn điểm do hủy đơn hàng #{orderId}",
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RedeemPointsAsync(int userId, int points, int? orderId, string description)
        {
            if (points <= 0) return false;

            var available = await GetAvailablePointsAsync(userId);
            if (available < points) return false;

            _context.LoyaltyPointTransactions.Add(new LoyaltyPointTransaction
            {
                UserId = userId,
                OrderId = orderId,
                Points = -points,
                Description = description,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }
    }
}