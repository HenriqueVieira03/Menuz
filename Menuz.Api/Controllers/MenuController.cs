using Menuz.Api.Data;
using Menuz.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menuz.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController(MenuzDbContext db) : ControllerBase
{
    /// <summary>Cardápio completo: categorias com seus produtos disponíveis (usado pelo tablet).</summary>
    [HttpGet]
    public async Task<ActionResult<List<CategoryMenuDto>>> Get(CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking()
            .OrderBy(c => c.DisplayOrder).ToListAsync(ct);

        var products = await db.Products.AsNoTracking()
            .Where(p => p.IsAvailable)
            .OrderBy(p => p.Name)
            .ProjectToDto()
            .ToListAsync(ct);

        var byCategory = products.ToLookup(p => p.CategoryId);

        return categories
            .Select(c => new CategoryMenuDto(c.Id, c.Name, c.DisplayOrder, byCategory[c.Id].ToList()))
            .ToList();
    }
}
