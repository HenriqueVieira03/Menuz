using Menuz.Api.Data;
using Menuz.Api.Dtos;
using Menuz.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menuz.Api.Controllers;

/// <summary>Contas das mesas para a gerência: ver as abertas e encerrar uma (cliente que esqueceu de fechar).</summary>
[ApiController]
[Route("api/admin/bills")]
[Authorize(Roles = Roles.Manager)]
public class AdminBillsController(MenuzDbContext db) : ControllerBase
{
    /// <summary>Contas abertas, uma por mesa, em ordem de mesa.</summary>
    [HttpGet("open")]
    public async Task<ActionResult<List<OpenBillDto>>> GetOpen(CancellationToken ct)
    {
        var bills = await db.Bills.AsNoTracking()
            .Include(b => b.Orders)
            .Where(b => b.Status == BillStatus.Open)
            .OrderBy(b => b.TableNumber)
            .ToListAsync(ct);

        return bills.Select(b => new OpenBillDto(
            b.Id,
            b.TableNumber,
            DateTime.SpecifyKind(b.OpenedAt, DateTimeKind.Utc),
            b.Orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total),
            b.Orders.Count(o => o.Status != OrderStatus.Cancelled),
            b.Orders.Count(o => o.Status is OrderStatus.Pending or OrderStatus.Preparing))).ToList();
    }

    /// <summary>
    /// Encerra a conta (pagamento simulado). Mesma regra do fechamento pelo cliente: não pode haver pedido
    /// Novo ou Em preparo (a gerência pode concluí-lo na aba de pedidos ou cancelá-lo antes).
    /// </summary>
    [HttpPost("{id:int}/close")]
    public async Task<ActionResult<BillDto>> Close(int id, CancellationToken ct)
    {
        var tableNumber = await db.Bills.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => (int?)b.TableNumber)
            .FirstOrDefaultAsync(ct);
        if (tableNumber is null) return NotFound("Conta não encontrada.");

        // Mesma trava usada ao criar pedidos e no fechamento pelo cliente.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.LockTableAsync(tableNumber.Value, ct);

        var bill = await db.Bills
            .Include(b => b.Orders).ThenInclude(o => o.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.Id == id, ct);
        if (bill is null) return NotFound("Conta não encontrada.");

        if (bill.Status != BillStatus.Open)
            return Conflict("Esta conta já foi encerrada.");

        if (bill.Orders.Any(o => o.Status is OrderStatus.Pending or OrderStatus.Preparing))
            return Conflict("Há pedidos em andamento nesta conta. Conclua ou cancele esses pedidos antes de encerrar.");

        bill.Status = BillStatus.Paid;
        bill.ClosedAt = DateTime.UtcNow;
        bill.TotalPaid = bill.Orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return bill.ToDto();
    }
}
