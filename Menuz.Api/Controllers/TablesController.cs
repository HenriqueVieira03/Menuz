using Menuz.Api.Data;
using Menuz.Api.Dtos;
using Menuz.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menuz.Api.Controllers;

/// <summary>
/// Endpoints do tablet da mesa (sem login, como a criação de pedidos): o cliente acompanha a própria conta.
/// </summary>
[ApiController]
[Route("api/tables")]
[AllowAnonymous]
public class TablesController(MenuzDbContext db) : ControllerBase
{
    /// <summary>Conta aberta da mesa, com os pedidos e seus status. 204 quando a mesa ainda não pediu nada.</summary>
    [HttpGet("{number:int}/bill")]
    public async Task<ActionResult<BillDto>> GetOpenBill(int number, CancellationToken ct)
    {
        if (number is < 1 or > OrderRules.MaxTableNumber)
            return BadRequest($"Número da mesa inválido (1 a {OrderRules.MaxTableNumber}).");

        var bill = await db.Bills.AsNoTracking()
            .Include(b => b.Orders).ThenInclude(o => o.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.TableNumber == number && b.Status == BillStatus.Open, ct);

        return bill is null ? NoContent() : bill.ToDto();
    }

    /// <summary>
    /// Fecha a conta da mesa (pagamento simulado). Regras: só fecha se não houver pedido Novo ou Em preparo
    /// e se o total enviado for o mesmo da conta no servidor (ninguém paga um valor diferente do que viu).
    /// </summary>
    [HttpPost("{number:int}/bill/close")]
    public async Task<ActionResult<BillDto>> CloseBill(int number, CloseBillRequest request, CancellationToken ct)
    {
        if (number is < 1 or > OrderRules.MaxTableNumber)
            return BadRequest($"Número da mesa inválido (1 a {OrderRules.MaxTableNumber}).");

        // Mesma trava usada ao criar pedidos: fechamento e novos pedidos da mesa nunca se atropelam.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.LockTableAsync(number, ct);

        var bill = await db.Bills
            .Include(b => b.Orders).ThenInclude(o => o.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.TableNumber == number && b.Status == BillStatus.Open, ct);

        if (bill is null)
            return NotFound("Esta mesa não tem conta aberta.");

        if (bill.Orders.Any(o => o.Status is OrderStatus.Pending or OrderStatus.Preparing))
            return Conflict("Ainda há pedidos em andamento. Aguarde a cozinha terminar para fechar a conta.");

        var total = bill.Orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total);
        if (total != request.ExpectedTotal)
            return Conflict("A conta mudou desde que você abriu o resumo. Confira o novo total e tente de novo.");

        bill.Status = BillStatus.Paid;
        bill.ClosedAt = DateTime.UtcNow;
        bill.TotalPaid = total;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return bill.ToDto();
    }
}
