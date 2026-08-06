using System.Threading.Tasks;
using System.Collections.Generic;

namespace MTKPM_Clothing_Store_web.Services
{
    public class LoyaltyTierInfo
    {
        public string TierName { get; set; } = "Thân thiết";
        public decimal DiscountPercent { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal? NextTierThreshold { get; set; }
        public string? NextTierName { get; set; }
    }

    public interface ILoyaltyService
    {
        Task<decimal> GetTotalSpentAsync(int userId);
        Task<LoyaltyTierInfo> GetTierInfoAsync(int userId);
        Task<int> GetAvailablePointsAsync(int userId);

        // Cộng điểm khi đơn chuyển sang Delivered. Trả về false nếu đơn này đã được cộng điểm trước đó (chống trùng).
        Task<bool> EarnPointsForOrderAsync(int userId, int orderId, decimal orderTotal);

        // Trừ lại điểm đã cộng cho 1 đơn (dùng khi đơn Delivered bị hủy ngược lại).
        Task<bool> ReversePointsForOrderAsync(int orderId);

        // Trừ điểm khi khách dùng điểm để giảm giá lúc checkout. Trả về false nếu không đủ điểm.
        Task<bool> RedeemPointsAsync(int userId, int points, int? orderId, string description);
    }
}