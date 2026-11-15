using System.Globalization;
using System.Text;
using Menuz.Api.Data;
using Menuz.Api.Dtos;
using Menuz.Api.Hubs;
using Menuz.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Menuz.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(MenuzDbContext db, IHubContext<OrderHub> hub) : ControllerBase
{
    /// <summary>Cria um pedido (tablet do cliente). Os preços vêm do banco, nunca do cliente.</summary>
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        if (request.Items is null || request.Items.Count == 0)
            return BadRequest("O pedido precisa ter ao menos um item.");

        if (request.Items.Any(i => i.Quantity <= 0))
            return BadRequest("Quantidade inválida.");

        var ids = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id) && p.IsAvailable)
            .ToDictionaryAsync(p => p.Id, ct);

        if (products.Count != ids.Count)
            return BadRequest("Há produtos inexistentes ou indisponíveis no pedido.");

        var order = new Order
        {
            ClientSentAt = request.ClientSentAt?.ToUniversalTime(),
            Items = request.Items
                .GroupBy(i => i.ProductId)
                .Select(g => new OrderItem
                {
                    ProductId = g.Key,
                    ProductName = products[g.Key].Name,
                    UnitPrice = products[g.Key].BasePrice,
                    Quantity = g.Sum(x => x.Quantity)
                }).ToList()
        };
        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        var dto = order.ToDto();

        // Tempo real: a cozinha recebe o pedido sem precisar atualizar a tela.
        // CancellationToken.None: o pedido já foi gravado, então a notificação não deve ser abortada.
        await hub.Clients.Group(OrderHub.KitchenGroup).SendAsync(OrderHub.NewOrderEvent, dto, CancellationToken.None);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, dto);
    }

    /// <summary>Pedidos em andamento (painel da cozinha). Use ?includeFinished=true para todos.</summary>
    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetAll([FromQuery] bool includeFinished, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();

        if (!includeFinished)
            query = query.Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled);

        var orders = await query.OrderBy(o => o.CreatedAt).ToListAsync(ct);
        return orders.Select(o => o.ToDto()).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
        return order is null ? NotFound() : order.ToDto();
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(int id, UpdateOrderStatusRequest request, CancellationToken ct)
    {
        if (!Enum.IsDefined(request.Status))
            return BadRequest("Status inválido.");

        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return NotFound();

        order.Status = request.Status;
        await db.SaveChangesAsync(ct);

        var dto = order.ToDto();

        // Mantém todas as telas da cozinha (e futuramente o PDV) com o mesmo status.
        await hub.Clients.Group(OrderHub.KitchenGroup).SendAsync(OrderHub.OrderUpdatedEvent, dto, CancellationToken.None);

        return dto;
    }

    /// <summary>
    /// Estatística descritiva dos tempos de entrega à cozinha (REQ. 5 / critério &lt; 2 s).
    /// Filtros opcionais: ?from=2026-11-15T00:00:00Z&amp;to=...
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult<OrderMetricsDto>> GetMetrics([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var rows = await LatencyQuery(from, to).ToListAsync(ct);
        var acked = rows.Where(r => r.KitchenReceivedAt != null).ToList();

        var serverToKitchen = acked.Select(r => (r.KitchenReceivedAt!.Value - r.CreatedAt).TotalMilliseconds);
        var endToEnd = acked.Where(r => r.ClientSentAt != null)
            .Select(r => (r.KitchenReceivedAt!.Value - r.ClientSentAt!.Value).TotalMilliseconds);

        return new OrderMetricsDto(rows.Count, acked.Count, Stats(serverToKitchen), Stats(endToEnd));
    }

    /// <summary>Exporta as medições em CSV (abre no Excel/Sheets para montar tabelas e gráficos do artigo).</summary>
    [HttpGet("latency.csv")]
    public async Task<IActionResult> ExportLatencyCsv([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var rows = await LatencyQuery(from, to).ToListAsync(ct);
        var inv = CultureInfo.InvariantCulture;

        var sb = new StringBuilder("OrderId;ClientSentAtUtc;ServerReceivedAtUtc;KitchenReceivedAtUtc;ServerToKitchenMs;EndToEndMs\n");
        foreach (var r in rows)
        {
            double? s2k = r.KitchenReceivedAt is { } k ? (k - r.CreatedAt).TotalMilliseconds : null;
            double? e2e = r.KitchenReceivedAt is { } k2 && r.ClientSentAt is { } c ? (k2 - c).TotalMilliseconds : null;

            sb.Append(r.Id).Append(';')
              .Append(r.ClientSentAt?.ToString("O", inv)).Append(';')
              .Append(r.CreatedAt.ToString("O", inv)).Append(';')
              .Append(r.KitchenReceivedAt?.ToString("O", inv)).Append(';')
              .Append(s2k?.ToString("F0", inv)).Append(';')
              .Append(e2e?.ToString("F0", inv)).Append('\n');
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "menuz-latencia.csv");
    }

    private IQueryable<LatencyRow> LatencyQuery(DateTime? from, DateTime? to)
    {
        var query = db.Orders.AsNoTracking();
        if (from is { } f) query = query.Where(o => o.CreatedAt >= f.ToUniversalTime());
        if (to is { } t) query = query.Where(o => o.CreatedAt <= t.ToUniversalTime());

        return query.OrderBy(o => o.Id)
            .Select(o => new LatencyRow(o.Id, o.ClientSentAt, o.CreatedAt, o.KitchenReceivedAt));
    }

    private static LatencyStatsDto Stats(IEnumerable<double> values)
    {
        var v = values.OrderBy(x => x).ToArray();
        if (v.Length == 0) return new LatencyStatsDto(0, 0, 0, 0, 0, 0);

        static double Percentile(double[] sorted, double p) =>
            sorted[(int)Math.Clamp(Math.Ceiling(p * sorted.Length) - 1, 0, sorted.Length - 1)];

        return new LatencyStatsDto(
            v.Length,
            Math.Round(v.Average(), 1),
            Math.Round(Percentile(v, 0.5), 1),
            Math.Round(Percentile(v, 0.95), 1),
            Math.Round(v[^1], 1),
            Math.Round(100.0 * v.Count(x => x < 2000) / v.Length, 1));
    }

    private record LatencyRow(int Id, DateTime? ClientSentAt, DateTime CreatedAt, DateTime? KitchenReceivedAt);
}
