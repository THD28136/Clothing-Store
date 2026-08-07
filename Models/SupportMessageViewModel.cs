using System.ComponentModel.DataAnnotations;

namespace MTKPM_Clothing_Store_web.Models
{
    public class SupportMessageViewModel
    {
        [Display(Name = "Họ tên")]
        [StringLength(100)]
        public string? Name { get; set; }

        [Display(Name = "Email")]
        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [Display(Name = "Mã đơn (tuỳ chọn)")]
        [StringLength(50)]
        public string? OrderNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung tin nhắn.")]
        [Display(Name = "Tin nhắn")]
        [StringLength(2000, ErrorMessage = "Nội dung quá dài.")]
        public string Message { get; set; } = default!;
    }
}