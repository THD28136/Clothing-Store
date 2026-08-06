using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MTKPM_Clothing_Store_web.Models;
using MTKPM_Clothing_Store_web.Services;

namespace MTKPM_Clothing_Store_web.Controllers;

public class PaymentsPayPalController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly PayPalService _paypal;
    private readonly ILogger<PaymentsPayPalController> _logger;

    public PaymentsPayPalController(ApplicationDbContext db, PayPalService paypal, ILogger<PaymentsPayPalController> logger)
    {
        _db = db;
        _paypal = paypal;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int orderId)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
            return NotFound();

        decimal totalVnd = order.TotalAmount ?? 0m;

        // Tỷ giá quy đổi (đồ án có thể cố định)
        const decimal ExchangeRate = 26000m;

        // Quy đổi sang USD
        decimal amount = Math.Round(totalVnd / ExchangeRate, 2);

        _logger.LogInformation("PayPal Amount: {Vnd} VND -> {Usd} USD", totalVnd, amount);

        var returnUrl = Url.Action(
            nameof(CaptureReturn),
            "PaymentsPayPal",
            null,
            Request.Scheme)!;

        var cancelUrl = Url.Action(
            nameof(Cancel),
            "PaymentsPayPal",
            null,
            Request.Scheme)!;

        var (paypalOrderId, approveUrl) =
            await _paypal.CreateOrderAsync(
                amount,
                returnUrl,
                cancelUrl);

        order.PaymentProviderId = paypalOrderId;
        order.Status = "PendingPayment";

        await _db.SaveChangesAsync();

        return Redirect(approveUrl);
    }

    [HttpGet]
    public async Task<IActionResult> CaptureReturn(string token)
    {
        var order = await _db.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o => o.PaymentProviderId == token);

        if (order == null)
            return NotFound();

        var (ok, raw) = await _paypal.CaptureOrderAsync(token);

        if (!ok)
            return BadRequest(raw);

        order.Status = "Paid";

        // NOTE: Do NOT decrement product.Stock here — stock was already updated by the database trigger
        // when order_details rows were created earlier in CheckoutPost. Modifying product.Stock here with
        // the trigger present will double-decrement and may cause negative stock / rollback.

        await _db.SaveChangesAsync();

        HttpContext.Session.Remove("Cart");

        return RedirectToAction(
            "Success",
            "Payments",
            new { id = order.OrderId });
    }

    [HttpGet]
    public async Task<IActionResult> Cancel(string token)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(x => x.PaymentProviderId == token);

        if (order != null)
        {
            order.Status = "Cancelled";
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(
            "Checkout",
            "Payments");
    }
}