using System;
using System.Collections.Generic;

namespace MTKPM_Clothing_Store_web.Models;

public partial class Coupon
{
    public int CouponId { get; set; }

    public string CouponName { get; set; } = null!;

    public string Code { get; set; } = null!;

    public decimal? DiscountPercent { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
