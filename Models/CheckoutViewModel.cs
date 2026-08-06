using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using MTKPM_Clothing_Store_web.Models;

namespace MTKPM_Clothing_Store_web.Models
{
    public class CheckoutItem
    {
        public Product Product { get; set; } = null!;
        public int Quantity { get; set; }

        public decimal SubTotal => Product?.Price * Quantity ?? 0m;
    }

    public enum PaymentMethodType
    {
        Card = 1,
        COD = 2,
        PayPal = 3,
        Momo = 4
    }

    public class CheckoutViewModel
    {
        public CheckoutViewModel()
        {
            Items = new List<CheckoutItem>();
            SelectedPaymentMethod = PaymentMethodType.Card;
        }

        public List<CheckoutItem> Items { get; set; }

        public decimal Total => Items.Sum(i => i.SubTotal);

        // Payment fields
        [Required(ErrorMessage = "Tên trên thẻ là bắt buộc.")]
        [StringLength(100)]
        public string CardName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số thẻ là bắt buộc.")]
        [RegularExpression(@"^[0-9\s\-]{12,23}$", ErrorMessage = "Số thẻ không hợp lệ.")]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tháng hết hạn là bắt buộc.")]
        [Range(1, 12, ErrorMessage = "Tháng phải ở giữa 1 và 12.")]
        [Display(Name = "Expiry Month")]
        public int ExpiryMonth { get; set; }

        [Required(ErrorMessage = "Năm hết hạn là bắt buộc.")]
        [Range(2023, 2100, ErrorMessage = "Năm hết hạn không hợp lệ.")]
        [Display(Name = "Expiry Year")]
        public int ExpiryYear { get; set; }

        [Required(ErrorMessage = "CVV/CVC là bắt buộc.")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV phải có 3 hoặc 4 chữ số.")]
        [Display(Name = "CVV/CVC")]
        public string CVV { get; set; } = string.Empty;

        // Payment method selection
        [Display(Name = "Phương thức thanh toán")]
        public PaymentMethodType SelectedPaymentMethod { get; set; }

        // New: will be populated from current user (or session) in controller
        public int? UserId { get; set; }

        // Guest checkout: required only when there's no logged-in user
        // (enforced in the controller, since that's where we know auth state).
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string? GuestEmail { get; set; }

        [Display(Name = "Họ tên")]
        [StringLength(100)]
        public string? GuestName { get; set; }

        // Optional guest phone (Order model has GuestPhone)
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string? GuestPhone { get; set; }

        // Địa chỉ giao hàng cho khách vãng lai (không có sổ địa chỉ).
        // Cấu trúc 2 cấp: Tỉnh/Thành phố -> Xã/Phường/Đặc khu (đúng theo cải cách hành chính từ 01/07/2025).
        [Display(Name = "Tỉnh/Thành phố")]
        public string? GuestProvince { get; set; }

        [Display(Name = "Xã/Phường")]
        public string? GuestWard { get; set; }

        [Display(Name = "Số nhà, tên đường")]
        [StringLength(255)]
        public string? GuestStreet { get; set; }

        // Coupon: user-entered code, plus what the server determined about it.
        // AppliedDiscountPercent/CouponMessage are set by the controller after
        // validation — never trust a discount value coming from the client.
        [Display(Name = "Mã giảm giá")]
        // Shipping address
        public int? SelectedAddressId { get; set; }

        public List<ShippingAddress> Addresses { get; set; } = new();

        public ShippingAddress? DefaultAddress =>
            Addresses.FirstOrDefault(a => a.IsDefault);
        public string? CouponCode { get; set; }

        public decimal? AppliedDiscountPercent { get; set; }

        public string? CouponMessage { get; set; }

        public decimal DiscountAmount => Total * (AppliedDiscountPercent ?? 0m) / 100m;

        // Khách hàng thân thiết: hạng thành viên (giảm % tự động) + điểm tích lũy.
        // Các giá trị hiển thị (TierName/TierDiscountPercent/AvailablePoints) luôn do server
        // tính lại và gán vào — không tin tưởng giá trị này nếu nó đến từ client.
        public string TierName { get; set; } = "Thân thiết";

        public decimal TierDiscountPercent { get; set; }

        public int AvailablePoints { get; set; }

        [Display(Name = "Số điểm muốn dùng")]
        [Range(0, int.MaxValue, ErrorMessage = "Số điểm không hợp lệ.")]
        public int PointsToRedeem { get; set; }

        public decimal TierDiscountAmount => Total * TierDiscountPercent / 100m;

        // 1 điểm = 1.000đ, không vượt quá điểm khả dụng và không vượt quá 50% giá trị đơn
        // (giới hạn cụ thể còn lại được enforce ở server trong CheckoutPost).
        public decimal PointsDiscountAmount => PointsToRedeem * 1000m;

        public decimal FinalTotal
        {
            get
            {
                var afterCoupon = Total - DiscountAmount;
                var afterTier = afterCoupon - TierDiscountAmount;
                var afterPoints = afterTier - PointsDiscountAmount;
                return afterPoints < 0 ? 0 : afterPoints;
            }
        }
    }
}