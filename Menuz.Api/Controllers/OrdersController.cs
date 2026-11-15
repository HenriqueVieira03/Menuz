using System.Globalization;
using System.Text;
using Menuz.Api.Data;
using Menuz.Api.Dtos;
using Menuz.Api.Hubs;
using Menuz.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Menuz.Api.Controllers;

/// <summary>
/// Pedidos. Criar é público (tablet da mesa, sem login); consultar e mudar status exige a equipe;
/// cancelar e ver métricas exige a gerência.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Staff)]
public class OrdersController(MenuzDbContext db, IHubContext<OrderHub> hub) : ControllerBase
{
    /// <summary>Cria um pedido (tablet do cliente). Os preços vêm do banco, nunca do cliente.</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        if (request.TableNumber is < 1 or > OrderRules.MaxTableNumber)
            return BadRequest($"Número da mesa inválido (1 a {OrderRules.MaxTableNumber}).");

        if (request.Items is null || request.Items.Count == 0)
            return BadRequest("O pedido precisa ter ao menos um item.");

        if (request.Items.Any(i => i.Quantity is <= 0 or > OrderRules.MaxQuantityPerItem))
            return BadRequest("Quantidade inválida.");

        var ids = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.AsNoTracking()
            .Include(p => p.Sizes)
            .Include(p => p.Addons)
            .Include(p => p.Ingredients)
            .AsSplitQuery()
            .Where(p => ids.Contains(p.Id) && p.IsAvailable)
            .ToDictionaryAsync(p => p.Id, ct);

        if (products.Count != ids.Count)
            return BadRequest("Há produtos inexistentes ou indisponíveis no pedido.");

        // Cada linha é mantida separada: o mesmo produto com escolhas diferentes não pode ser somado.
        var items = new List<OrderItem>();
        foreach (var line in request.Items)
        {
            var (item, error) = BuildItem(products[line.ProductId], line);
            if (error is not null) return BadRequest(error);
            items.Add(item!);
        }

        // Todo pedido entra na conta aberta da mesa (e abre uma nova se não houver).
        var bill = await GetOrOpenBillAsync(request.TableNumber, ct);

        var order = new Order
        {
            BillId = bill.Id,
            TableNumber = request.TableNumber,
            ClientSentAt = request.ClientSentAt?.ToUniversalTime(),
            Items = items
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

    /// <summary>
    /// Pedidos em andamento (painel da cozinha). Use ?includeFinished=true para todos (painel da gerência)
    /// e ?from=/&amp;to= (UTC) para limitar o período.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetAll(
        [FromQuery] bool includeFinished, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();

        if (!includeFinished)
            query = query.Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled);
        if (from is { } f)
            query = query.Where(o => o.CreatedAt >= f.ToUniversalTime());
        if (to is { } t)
            query = query.Where(o => o.CreatedAt < t.ToUniversalTime());

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

        // Cancelar é decisão da gerência; a cozinha só faz o pedido andar.
        if (request.Status == OrderStatus.Cancelled && !User.IsInRole(Roles.Manager))
            return Forbid();

        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return NotFound();

        if (order.Status is OrderStatus.Delivered or OrderStatus.Cancelled && order.Status != request.Status)
            return BadRequest("Pedido já finalizado; o status não pode mais ser alterado.");

        order.SetStatus(request.Status, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);

        var dto = order.ToDto();

        // Mantém todas as telas da equipe (cozinha e gerência) com o mesmo status.
        await hub.Clients.Group(OrderHub.KitchenGroup).SendAsync(OrderHub.OrderUpdatedEvent, dto, CancellationToken.None);

        return dto;
    }

    /// <summary>
    /// Estatística descritiva dos tempos de entrega à cozinha (REQ. 5 / critério &lt; 2 s).
    /// Filtros opcionais: ?from=2026-11-15T00:00:00Z&amp;to=...
    /// </summary>
    [HttpGet("metrics")]
    [Authorize(Roles = Roles.Manager)]
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
    [Authorize(Roles = Roles.Manager)]
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

    /// <summary>
    /// Valida as escolhas do cliente contra o cadastro do produto e calcula o preço unitário no servidor.
    /// </summary>
    private static (OrderItem? Item, string? Error) BuildItem(Product product, CreateOrderItemRequest line)
    {
        ProductSize? size = null;
        if (product.Sizes.Count > 0)
        {
            if (line.SizeId is null)
                return (null, $"Escolha o tamanho de \"{product.Name}\".");

            size = product.Sizes.FirstOrDefault(s => s.Id == line.SizeId);
            if (size is null)
                return (null, $"Tamanho inválido para \"{product.Name}\".");
        }
        else if (line.SizeId is not null)
        {
            return (null, $"\"{product.Name}\" não possui tamanhos.");
        }

        var addonIds = (line.AddonIds ?? new()).Distinct().ToList();
        var addons = product.Addons.Where(a => addonIds.Contains(a.Id)).OrderBy(a => a.Name).ToList();
        if (addons.Count != addonIds.Count)
            return (null, $"Adicional inválido para \"{product.Name}\".");

        var removedIds = (line.RemovedIngredientIds ?? new()).Distinct().ToList();
        var removed = product.Ingredients.Where(i => removedIds.Contains(i.Id) && i.IsRemovable).OrderBy(i => i.Name).ToList();
        if (removed.Count != removedIds.Count)
            return (null, $"Ingrediente inválido ou não removível em \"{product.Name}\".");

        var notes = string.IsNullOrWhiteSpace(line.Notes) ? null : line.Notes.Trim();
        if (notes?.Length > OrderRules.NotesMaxLength)
            return (null, $"A observação pode ter no máximo {OrderRules.NotesMaxLength} caracteres.");

        return (new OrderItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            SizeName = size?.Name,
            Addons = addons.Select(a => a.Name).ToList(),
            RemovedIngredients = removed.Select(i => i.Name).ToList(),
            Notes = notes,
            UnitPrice = product.BasePrice + (size?.PriceDelta ?? 0) + addons.Sum(a => a.Price),
            Quantity = line.Quantity
        }, null);
    }

    /// <summary>
    /// Devolve a conta aberta da mesa ou cria uma. Se dois tablets da mesma mesa pedirem ao mesmo tempo,
    /// o índice único do banco recusa a segunda conta e usamos a que o outro tablet acabou de abrir.
    /// </summary>
    private async Task<Bill> GetOrOpenBillAsync(int tableNumber, CancellationToken ct)
    {
        var bill = await db.Bills.FirstOrDefaultAsync(
            b => b.TableNumber == tableNumber && b.Status == BillStatus.Open, ct);
        if (bill is not null) return bill;

        bill = new Bill { TableNumber = tableNumber };
        db.Bills.Add(bill);
        try
        {
            await db.SaveChangesAsync(ct);
            return bill;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.Entry(bill).State = EntityState.Detached;
            return await db.Bills.FirstAsync(
                b => b.TableNumber == tableNumber && b.Status == BillStatus.Open, ct);
        }
    }

    private IQueryable<LatencyRow> LatencyQuery(DateTime? from, DateTime? to)
    {
        var query = db.Orders.AsNoTracking();
        if (from is { } f) query = query.Where(o => o.CreatedAt >= f.ToUniversalTime());
        if (to is { } t) query = query.Where(o => o.CreatedAt <= t.ToUniversalTime());

        return query.OrderBy(o => o.Id)
            .Select(o => new LatencyRow(o.Id, o.ClientSentAt, o.CreatedAt, o.KitchenReceivedAt));
    }

    /// <summary>Média, mediana, P95, máximo e % abaixo de 2 s (também usado pelo painel da gerência).</summary>
    internal static LatencyStatsDto Stats(IEnumerable<double> values)
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
