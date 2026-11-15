using Menuz.Api.Data;
using Menuz.Api.Dtos;
using Menuz.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menuz.Api.Controllers;

/// <summary>Gestão do cardápio pela gerência (PDV): categorias, produtos e suas opções.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Manager)]
public class AdminMenuController(MenuzDbContext db) : ControllerBase
{
    // ================= Categorias =================

    [HttpGet("categories")]
    public async Task<ActionResult<List<CategoryAdminDto>>> GetCategories(CancellationToken ct) =>
        await db.Categories.AsNoTracking()
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryAdminDto(c.Id, c.Name, c.DisplayOrder, c.Products.Count))
            .ToListAsync(ct);

    [HttpPost("categories")]
    public async Task<ActionResult<CategoryAdminDto>> CreateCategory(SaveCategoryRequest request, CancellationToken ct)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (await ValidateCategoryAsync(name, null, ct) is { } error) return BadRequest(error);

        var category = new Category { Name = name, DisplayOrder = request.DisplayOrder };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return new CategoryAdminDto(category.Id, category.Name, category.DisplayOrder, 0);
    }

    [HttpPut("categories/{id:int}")]
    public async Task<ActionResult<CategoryAdminDto>> UpdateCategory(int id, SaveCategoryRequest request, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null) return NotFound();

        var name = request.Name?.Trim() ?? string.Empty;
        if (await ValidateCategoryAsync(name, id, ct) is { } error) return BadRequest(error);

        category.Name = name;
        category.DisplayOrder = request.DisplayOrder;
        await db.SaveChangesAsync(ct);

        var count = await db.Products.CountAsync(p => p.CategoryId == id, ct);
        return new CategoryAdminDto(category.Id, category.Name, category.DisplayOrder, count);
    }

    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null) return NotFound();

        if (await db.Products.AnyAsync(p => p.CategoryId == id, ct))
            return Conflict("A categoria ainda tem produtos. Mova ou exclua os produtos antes.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<string?> ValidateCategoryAsync(string name, int? currentId, CancellationToken ct)
    {
        if (name.Length is 0 or > 80) return "O nome da categoria deve ter de 1 a 80 caracteres.";

        var lower = name.ToLower();
        var duplicated = await db.Categories.AnyAsync(c => c.Name.ToLower() == lower && c.Id != currentId, ct);
        return duplicated ? "Já existe uma categoria com esse nome." : null;
    }

    // ================= Produtos =================

    /// <summary>Todos os produtos (inclusive indisponíveis), com as opções.</summary>
    [HttpGet("products")]
    public async Task<ActionResult<List<ProductAdminDto>>> GetProducts([FromQuery] int? categoryId, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking();
        if (categoryId is { } cid) query = query.Where(p => p.CategoryId == cid);

        return await query
            .OrderBy(p => p.Category!.DisplayOrder).ThenBy(p => p.Name)
            .Select(ToAdminDto)
            .ToListAsync(ct);
    }

    [HttpGet("products/{id:int}")]
    public async Task<ActionResult<ProductAdminDto>> GetProduct(int id, CancellationToken ct)
    {
        var dto = await db.Products.AsNoTracking().Where(p => p.Id == id).Select(ToAdminDto).FirstOrDefaultAsync(ct);
        return dto is null ? NotFound() : dto;
    }

    [HttpPost("products")]
    public async Task<ActionResult<ProductAdminDto>> CreateProduct(SaveProductRequest request, CancellationToken ct)
    {
        if (await ValidateProductAsync(request, ct) is { } error) return BadRequest(error);

        var product = new Product();
        if (ApplyProduct(product, request) is { } optionError) return BadRequest(optionError);

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, await LoadDtoAsync(product.Id, ct));
    }

    /// <summary>Atualiza o produto e sincroniza as opções (remove as que não vieram, atualiza e cria).</summary>
    [HttpPut("products/{id:int}")]
    public async Task<ActionResult<ProductAdminDto>> UpdateProduct(int id, SaveProductRequest request, CancellationToken ct)
    {
        var product = await db.Products
            .Include(p => p.Sizes).Include(p => p.Addons).Include(p => p.Ingredients)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return NotFound();

        if (await ValidateProductAsync(request, ct) is { } error) return BadRequest(error);
        if (ApplyProduct(product, request) is { } optionError) return BadRequest(optionError);

        await db.SaveChangesAsync(ct);
        return await LoadDtoAsync(id, ct);
    }

    /// <summary>Liga/desliga o produto no cardápio (ex.: acabou o ingrediente).</summary>
    [HttpPut("products/{id:int}/availability")]
    public async Task<ActionResult<ProductAdminDto>> SetAvailability(int id, SetAvailabilityRequest request, CancellationToken ct)
    {
        var updated = await db.Products.Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsAvailable, request.IsAvailable), ct);
        return updated == 0 ? NotFound() : await LoadDtoAsync(id, ct);
    }

    /// <summary>Exclui o produto. Pedidos antigos não são afetados (guardam nome e preço do momento da compra).</summary>
    [HttpDelete("products/{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken ct)
    {
        var deleted = await db.Products.Where(p => p.Id == id).ExecuteDeleteAsync(ct);
        return deleted == 0 ? NotFound() : NoContent();
    }

    private async Task<string?> ValidateProductAsync(SaveProductRequest r, CancellationToken ct)
    {
        var name = r.Name?.Trim() ?? string.Empty;
        if (name.Length is 0 or > 120) return "O nome do produto deve ter de 1 a 120 caracteres.";
        if ((r.Description?.Length ?? 0) > 500) return "A descrição pode ter no máximo 500 caracteres.";
        if (r.BasePrice is < 0 or > 99999) return "Preço inválido.";

        if (!string.IsNullOrWhiteSpace(r.ImageUrl))
        {
            if (r.ImageUrl.Length > 500 ||
                !Uri.TryCreate(r.ImageUrl.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return "A URL da imagem deve começar com http:// ou https://.";
        }

        if (!await db.Categories.AnyAsync(c => c.Id == r.CategoryId, ct))
            return "Categoria inválida.";

        var sizes = r.Sizes ?? new();
        var addons = r.Addons ?? new();
        var ingredients = r.Ingredients ?? new();

        if (CheckNames(sizes.Select(s => s.Name), "tamanho") is { } e1) return e1;
        if (CheckNames(addons.Select(a => a.Name), "adicional") is { } e2) return e2;
        if (CheckNames(ingredients.Select(i => i.Name), "ingrediente") is { } e3) return e3;

        if (sizes.Any(s => r.BasePrice + s.PriceDelta < 0 || s.PriceDelta > 99999))
            return "O acréscimo de um tamanho não pode deixar o preço negativo.";
        if (addons.Any(a => a.Price is < 0 or > 99999))
            return "Preço de adicional inválido.";

        return null;
    }

    private static string? CheckNames(IEnumerable<string?> names, string label)
    {
        var list = names.Select(n => n?.Trim() ?? string.Empty).ToList();
        if (list.Any(n => n.Length is 0 or > 60)) return $"Todo {label} precisa de um nome (até 60 caracteres).";
        if (list.Count != list.Distinct(StringComparer.OrdinalIgnoreCase).Count()) return $"Há {label}s com nomes repetidos.";
        return null;
    }

    /// <summary>Copia os dados do request para a entidade e sincroniza as listas de opções.</summary>
    private static string? ApplyProduct(Product p, SaveProductRequest r)
    {
        p.Name = r.Name.Trim();
        p.Description = r.Description?.Trim() ?? string.Empty;
        p.BasePrice = r.BasePrice;
        p.ImageUrl = string.IsNullOrWhiteSpace(r.ImageUrl) ? null : r.ImageUrl.Trim();
        p.IsAvailable = r.IsAvailable;
        p.CategoryId = r.CategoryId;

        return Sync(p.Sizes, r.Sizes, s => s.Id, x => x.Id,
                   (e, x) => { e.Name = x.Name.Trim(); e.PriceDelta = x.PriceDelta; }, "tamanho")
            ?? Sync(p.Addons, r.Addons, a => a.Id, x => x.Id,
                   (e, x) => { e.Name = x.Name.Trim(); e.Price = x.Price; }, "adicional")
            ?? Sync(p.Ingredients, r.Ingredients, i => i.Id, x => x.Id,
                   (e, x) => { e.Name = x.Name.Trim(); e.IsRemovable = x.IsRemovable; }, "ingrediente");
    }

    /// <summary>
    /// Sincroniza uma lista de opções: remove as que sumiram, atualiza as que vieram com Id e cria as sem Id.
    /// Manter os ids evita que um tablet com o cardápio em cache envie um tamanho "inexistente".
    /// </summary>
    private static string? Sync<TEntity, TRequest>(
        List<TEntity> current, List<TRequest>? requested,
        Func<TEntity, int> entityId, Func<TRequest, int?> requestId,
        Action<TEntity, TRequest> copy, string label)
        where TEntity : class, new()
    {
        requested ??= new();

        var ids = requested.Where(x => requestId(x) is not null).Select(x => requestId(x)!.Value).ToHashSet();
        if (ids.Any(id => current.All(e => entityId(e) != id)))
            return $"Há um {label} que não pertence a este produto.";

        current.RemoveAll(e => !ids.Contains(entityId(e)));

        foreach (var x in requested)
        {
            var entity = requestId(x) is { } id ? current.First(e => entityId(e) == id) : default;
            if (entity is null)
            {
                entity = new TEntity();
                current.Add(entity);
            }
            copy(entity, x);
        }

        return null;
    }

    private async Task<ProductAdminDto> LoadDtoAsync(int id, CancellationToken ct) =>
        await db.Products.AsNoTracking().Where(p => p.Id == id).Select(ToAdminDto).FirstAsync(ct);

    private static readonly System.Linq.Expressions.Expression<Func<Product, ProductAdminDto>> ToAdminDto = p =>
        new ProductAdminDto(
            p.Id, p.Name, p.Description, p.BasePrice, p.ImageUrl, p.IsAvailable, p.CategoryId, p.Category!.Name,
            p.Sizes.OrderBy(s => s.PriceDelta).ThenBy(s => s.Id).Select(s => new ProductSizeDto(s.Id, s.Name, s.PriceDelta)).ToList(),
            p.Addons.OrderBy(a => a.Name).Select(a => new ProductAddonDto(a.Id, a.Name, a.Price)).ToList(),
            p.Ingredients.OrderBy(i => i.Name).Select(i => new ProductIngredientDto(i.Id, i.Name, i.IsRemovable)).ToList());
}
