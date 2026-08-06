using System;
using System.Collections.Generic;

namespace MTKPM_Clothing_Store_web.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int? UserId { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? Status { get; set; }

    public int? CouponId { get; set; }

    public int? PaymentMethodId { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? GuestName { get; set; }

    public string? GuestPhone { get; set; }

    public string? GuestEmail { get; set; }

    public string? PaymentProviderId { get; set; }

    // Địa chỉ giao hàng: user có tài khoản -> tham chiếu tới 1 địa chỉ đã lưu trong sổ địa chỉ.
    public int? AddressId { get; set; }

    // Khách vãng lai không có sổ địa chỉ -> lưu trực tiếp dạng text (2 cấp: Tỉnh/Thành -> Xã/Phường).
    public string? GuestProvince { get; set; }

    public string? GuestWard { get; set; }

    public string? GuestStreet { get; set; }

    // Khách hàng thân thiết: số điểm đã dùng để giảm giá cho đơn này (và số tiền tương ứng).
    public int? PointsRedeemed { get; set; }

    public decimal? PointsDiscountAmount { get; set; }

    public virtual Coupon? Coupon { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual ShippingAddress? Address { get; set; }

    public virtual User? User { get; set; }
}