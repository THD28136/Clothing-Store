using System;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MTKPM_Clothing_Store_web.Models;

public partial class LoyaltyPointTransaction
{
    public int TransactionId { get; set; }

    public int UserId { get; set; }

    public int? OrderId { get; set; }

    // Dương = tích điểm (earn), âm = dùng điểm (redeem).
    public int Points { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ValidateNever]
    public virtual User User { get; set; } = null!;

    [ValidateNever]
    public virtual Order? Order { get; set; }
}