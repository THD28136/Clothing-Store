using System;

namespace MTKPM_Clothing_Store_web.Models
{
    // Session-only cart item model to avoid name collision with EF CartItem entity.
    public class SessionCartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Pic { get; set; }

        public decimal LineTotal => Price * Quantity;
    }
}   