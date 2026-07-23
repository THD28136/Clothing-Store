using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MTKPM_Clothing_Store_web.Models;

namespace MTKPM_Clothing_Store_web.Services;

public class PayPalService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly PayPalSettings _settings;

    // cached token + expiry (simple in-memory cache per instance)
    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;

    public PayPalService(IHttpClientFactory httpFactory, IOptions<PayPalSettings> opts)
    {
        _httpFactory = httpFactory;
        _settings = opts.Value;
    }

    private string BaseUrl => (_settings.Mode ?? "sandbox").Trim().ToLower() switch
    {
        "live" => "https://api-m.paypal.com",
        _ => "https://api-m.sandbox.paypal.com"
    };

    private async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _accessTokenExpiresAt)
            return _accessToken;

        var client = _httpFactory.CreateClient();
        var tokenUrl = $"{BaseUrl}/v1/oauth2/token";
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.Secret}"));

        using var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        req.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        _accessToken = root.GetProperty("access_token").GetString();
        var expiresIn = root.TryGetProperty("expires_in", out var exEl) ? exEl.GetInt32() : 3000;
        _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);

        if (string.IsNullOrEmpty(_accessToken)) throw new InvalidOperationException("Failed to obtain PayPal access token.");

        return _accessToken!;
    }

    // Create PayPal order and return PayPal order id
    public async Task<(string paypalOrderId, string? approveUrl)> CreateOrderAsync(decimal amount, string returnUrl, string cancelUrl, string? currency = null)
    {
        currency ??= _settings.Currency ?? "USD";
        var token = await GetAccessTokenAsync();

        var client = _httpFactory.CreateClient();
        var url = $"{BaseUrl}/v2/checkout/orders";

        var body = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    amount = new
                    {
                        currency_code = currency,
                        value = Math.Round(amount, 2).ToString("F2")
                    }
                }
            },
            application_context = new
            {
                return_url = returnUrl,
                cancel_url = cancelUrl,
                user_action = "PAY_NOW"
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var resp = await client.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = root.GetProperty("id").GetString() ?? throw new InvalidOperationException("Missing PayPal order id.");
        string? approve = null;
        if (root.TryGetProperty("links", out var links))
        {
            foreach (var link in links.EnumerateArray())
            {
                if (link.GetProperty("rel").GetString() == "approve")
                {
                    approve = link.GetProperty("href").GetString();
                    break;
                }
            }
        }

        return (id, approve);
    }

    // Capture a PayPal order (server-side capture)
    public async Task<(bool success, string rawResponse)> CaptureOrderAsync(string paypalOrderId)
    {
        if (string.IsNullOrWhiteSpace(paypalOrderId)) return (false, "empty id");
        var token = await GetAccessTokenAsync();
        var client = _httpFactory.CreateClient();
        var url = $"{BaseUrl}/v2/checkout/orders/{Uri.EscapeDataString(paypalOrderId)}/capture";

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        using var resp = await client.SendAsync(req);
        var json = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            return (false, json);

        using var doc = JsonDocument.Parse(json);
        var status = doc.RootElement.GetProperty("status").GetString() ?? string.Empty;
        var ok = string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase);

        return (ok, json);
    }
}