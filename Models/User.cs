using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MTKPM_Clothing_Store_web.Models;

public partial class User
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "Tên là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Tên không được vượt quá {1} ký tự.")]
    [Display(Name = "Họ và tên")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Email là bắt buộc.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(255, ErrorMessage = "Email không được vượt quá {1} ký tự.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = null!;

    [StringLength(50)]
    [Display(Name = "Vai trò")]
    public string? Role { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
