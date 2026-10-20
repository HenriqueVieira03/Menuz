using Menuz.Api.Data;
using Menuz.Api.Dtos;
using Menuz.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menuz.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(MenuzDbContext db) : ControllerBase
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

        // Próxima sprint (SignalR): notificar a cozinha aqui via IHubContext<OrderHub>.

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order.ToDto());
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
        return order.ToDto();
    }
}