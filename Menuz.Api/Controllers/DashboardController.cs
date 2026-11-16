using Menuz.Api.Data;
using Menuz.Api.Dtos;
using Menuz.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menuz.Api.Controllers;

/// <summary>Indicadores de atendimento para a gerência (REQ. 3).</summary>
[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = Roles.Manager)]
public class DashboardController(MenuzDbContext db) : ControllerBase
{
    /// <summary>
    /// Resumo do período [from, to) em UTC. Sem parâmetros, usa as últimas 24 horas.
    /// O app envia o início e o fim do "hoje" no fuso do tablet.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var end = (to ?? DateTime.UtcNow).ToUniversalTime();
        var start = (from ?? end.AddDays(-1)).ToUniversalTime();
        if (start >= end) return BadRequest("Período inválido.");

        var orders = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= start && o.CreatedAt < end)
            .ToListAsync(ct);

        var valid = orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();
        var revenue = valid.Sum(o => o.Total);

        static double? AverageMinutes(IEnumerable<TimeSpan> spans)
        {
            var list = spans.ToList();
            return list.Count == 0 ? null : Math.Round(list.Average(s => s.TotalMinutes), 1);
        }

        var top = valid.SelectMany(o => o.Items)
            .GroupBy(i => i.ProductName)
            .Select(g => new TopProductDto(g.Key, g.Sum(i => i.Quantity), g.Sum(i => i.UnitPrice * i.Quantity)))
            .OrderByDescending(t => t.Quantity).ThenByDescending(t => t.Revenue)
            .Take(5)
            .ToList();

        var latency = orders
            .Where(o => o.KitchenReceivedAt != null)
            .Select(o => (o.KitchenReceivedAt!.Value - o.CreatedAt).TotalMilliseconds);

        // Contas pagas no período (ticket médio por conta = total pago / contas fechadas).
        var closedBills = await db.Bills.AsNoTracking()
            .Where(b => b.Status == BillStatus.Paid && b.ClosedAt >= start && b.ClosedAt < end)
            .Select(b => b.TotalPaid ?? 0m)
            .ToListAsync(ct);

        return new DashboardDto(
            start,
            end,
            OrdersCount: valid.Count,
            CancelledCount: orders.Count - valid.Count,
            OpenCount: valid.Count(o => o.Status is OrderStatus.Pending or OrderStatus.Preparing or OrderStatus.Ready),
            Revenue: revenue,
            AverageTicket: valid.Count == 0 ? 0 : Math.Round(revenue / valid.Count, 2),
            AverageMinutesToReady: AverageMinutes(valid.Where(o => o.ReadyAt != null).Select(o => o.ReadyAt!.Value - o.CreatedAt)),
            AverageMinutesToDeliver: AverageMinutes(valid.Where(o => o.DeliveredAt != null).Select(o => o.DeliveredAt!.Value - o.CreatedAt)),
            TopProducts: top,
            KitchenLatency: OrdersController.Stats(latency),
            BillsClosedCount: closedBills.Count,
            AverageBillTotal: closedBills.Count == 0 ? 0 : Math.Round(closedBills.Sum() / closedBills.Count, 2));
    }
}
