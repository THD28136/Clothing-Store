using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MTKPM_Clothing_Store_web.Models;
using MTKPM_Clothing_Store_web.ViewModels;
using System.Data;
using System.Data.Common;

namespace MTKPM_Clothing_Store_web.ViewComponents;

public class DbFunctionsViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DbFunctionsViewComponent> _logger;

    public DbFunctionsViewComponent(ApplicationDbContext context, ILogger<DbFunctionsViewComponent> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var vm = new DbFunctionsViewModel();

        try
        {
            // read-only package procs (safe for non-admin)
            vm.TotalOrders = await ExecuteScalarIntAsync("EXEC pkg_order.sp6", null);
            vm.TotalProducts = await ExecuteScalarIntAsync("EXEC pkg_order.sp7", null);
            vm.ServerNow = await ExecuteScalarDateTimeAsync("EXEC pkg_order.sp8", null);

            // revenue (read-only) - exclude cancelled orders
            vm.TotalRevenue = await _context.Orders
                .AsNoTracking()
                .Where(o => o.TotalAmount.HasValue && (o.Status != "Cancelled"))
                .Select(o => o.TotalAmount!.Value)
                .SumAsync();

            // views via EF
            vm.ProductCount = await _context.VwProductLists.AsNoTracking().CountAsync();
            vm.InStockCount = await _context.VwInStockProducts.AsNoTracking().CountAsync();
            vm.RecentOrdersCount = await _context.VwRecentOrders.AsNoTracking().CountAsync();

            vm.TopSelling = await _context.VwTopSellingProducts.AsNoTracking()
                .OrderByDescending(t => t.TotalSold)
                .Take(5)
                .Select(t => new TopProduct { Name = t.Name, TotalSold = t.TotalSold ?? 0 })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DbFunctionsViewComponent failed to load DB info");
            vm.Error = "Unable to load database summary.";
        }

        return View(vm);
    }

    private async Task<decimal?> ExecuteScalarDecimalAsync(string sql, SqlParameter[]? parameters)
    {
        await using var cmd = _context.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = CommandType.Text;
        if (parameters != null) foreach (var p in parameters) cmd.Parameters.Add(p);

        if (cmd.Connection.State != ConnectionState.Open) await cmd.Connection.OpenAsync();

        var obj = await cmd.ExecuteScalarAsync();
        if (obj == null || obj == DBNull.Value) return null;
        return Convert.ToDecimal(obj);
    }

    private async Task<int?> ExecuteScalarIntAsync(string sql, SqlParameter[]? parameters)
    {
        await using var cmd = _context.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = CommandType.Text;
        if (parameters != null) foreach (var p in parameters) cmd.Parameters.Add(p);

        if (cmd.Connection.State != ConnectionState.Open) await cmd.Connection.OpenAsync();

        var obj = await cmd.ExecuteScalarAsync();
        if (obj == null || obj == DBNull.Value) return null;
        return Convert.ToInt32(obj);
    }

    private async Task<DateTime?> ExecuteScalarDateTimeAsync(string sql, SqlParameter[]? parameters)
    {
        await using var cmd = _context.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = CommandType.Text;
        if (parameters != null) foreach (var p in parameters) cmd.Parameters.Add(p);

        if (cmd.Connection.State != ConnectionState.Open) await cmd.Connection.OpenAsync();

        var obj = await cmd.ExecuteScalarAsync();
        if (obj == null || obj == DBNull.Value) return null;
        return Convert.ToDateTime(obj);
    }
}