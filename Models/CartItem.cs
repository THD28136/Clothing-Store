using System;

namespace MTKPM_Clothing_Store_web.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Pic { get; set; }

        public decimal LineTotal => Price * Quantity;
    }
}