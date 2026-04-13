using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace MTKPM_Clothing_Store_web.Models
{
    // Attach metadata to the generated partial User class so we don't change the scaffolded file.
    [ModelMetadataType(typeof(UserMetadata))]
    public partial class User
    {
    }

    public class UserMetadata
    {
        [Required(ErrorMessage = "Tên là bắt buộc.")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất {1} ký tự.")]
        public string Password { get; set; } = null!;

        public string? Role { get; set; }
    }
}