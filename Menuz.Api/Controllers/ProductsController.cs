using Menuz.Api.Data;
using Menuz.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menuz.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(MenuzDbContext db) : ControllerBase
{
    /// <summary>Lista produtos disponíveis; filtra por categoria quando informada.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll([FromQuery] int? categoryId, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking().Where(p => p.IsAvailable);
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        return await query.OrderBy(p => p.Name).ProjectToDto().ToListAsync(ct);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .ProjectToDto()
            .FirstOrDefaultAsync(ct);

        return product is null ? NotFound() : product;
    }
}
