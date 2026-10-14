using Menuz.Api.Data;
using Menuz.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menuz.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(MenuzDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll(CancellationToken ct) =>
        await db.Categories.AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto(c.Id, c.Name, c.DisplayOrder))
            .ToListAsync(ct);
}
