using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MTKPM_Clothing_Store_web.Models;

public partial class ShippingAddress
{
    public int AddressId { get; set; }

    public int UserId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
    [StringLength(100)]
    [Display(Name = "Họ tên người nhận")]
    public string ReceiverName { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [Display(Name = "Số điện thoại")]
    public string PhoneNumber { get; set; } = null!;

    // Cấu trúc hành chính 2 cấp (từ 01/07/2025): Tỉnh/Thành phố -> Xã/Phường/Đặc khu.
    [Required(ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố.")]
    [Display(Name = "Tỉnh/Thành phố")]
    public string Province { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng chọn Xã/Phường.")]
    [Display(Name = "Xã/Phường")]
    public string Ward { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập số nhà, tên đường.")]
    [StringLength(255)]
    [Display(Name = "Số nhà, tên đường")]
    public string Street { get; set; } = null!;

    [Display(Name = "Loại địa chỉ")]
    public string? AddressLabel { get; set; }

    public bool IsDefault { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ValidateNever]
    public virtual User User { get; set; } = null!;

    [ValidateNever]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    // Chuỗi địa chỉ đầy đủ dùng để hiển thị nhanh (không lưu trong DB).
    public string FullAddress => $"{Street}, {Ward}, {Province}";
}