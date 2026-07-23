namespace MTKPM_Clothing_Store_web.Models;

public class PayPalSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string Mode { get; set; } = "sandbox"; // "live" for production
    public string Currency { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 26000m;

}