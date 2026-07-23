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

        // Coupon: user-entered code, plus what the server determined about it.
        // AppliedDiscountPercent/CouponMessage are set by the controller after
        // validation — never trust a discount value coming from the client.
        [Display(Name = "Mã giảm giá")]
        public string? CouponCode { get; set; }

        public decimal? AppliedDiscountPercent { get; set; }

        public string? CouponMessage { get; set; }

        public decimal DiscountAmount => Total * (AppliedDiscountPercent ?? 0m) / 100m;

        public decimal FinalTotal => Total - DiscountAmount;
    }
}