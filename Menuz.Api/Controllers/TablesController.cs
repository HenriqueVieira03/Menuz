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
}
