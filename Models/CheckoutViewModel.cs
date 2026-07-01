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

    public class CheckoutViewModel
    {
        public CheckoutViewModel()
        {
            Items = new List<CheckoutItem>();
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

        // New: will be populated from current user (or session) in controller
        public int? UserId { get; set; }
    }
}