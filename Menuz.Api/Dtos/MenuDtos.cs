using Menuz.Api.Models;

namespace Menuz.Api.Dtos;

public record ProductSizeDto(int Id, string Name, decimal PriceDelta);
public record ProductAddonDto(int Id, string Name, decimal Price);
public record ProductIngredientDto(int Id, string Name, bool IsRemovable);

public record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal BasePrice,
    string? ImageUrl,
    int CategoryId,
    List<ProductSizeDto> Sizes,
    List<ProductAddonDto> Addons,
    List<ProductIngredientDto> Ingredients);

public record CategoryDto(int Id, string Name, int DisplayOrder);

public record CategoryMenuDto(int Id, string Name, int DisplayOrder, List<ProductDto> Products);

public static class ProductQueries
{
    /// <summary>Projeção traduzida para SQL pelo EF Core (sem carregar entidades completas).</summary>
    public static IQueryable<ProductDto> ProjectToDto(this IQueryable<Product> query) =>
        query.Select(p => new ProductDto(
            p.Id,
            p.Name,
            p.Description,
            p.BasePrice,
            p.ImageUrl,
            p.CategoryId,
            p.Sizes.OrderBy(s => s.PriceDelta).Select(s => new ProductSizeDto(s.Id, s.Name, s.PriceDelta)).ToList(),
            p.Addons.OrderBy(a => a.Name).Select(a => new ProductAddonDto(a.Id, a.Name, a.Price)).ToList(),
            p.Ingredients.OrderBy(i => i.Name).Select(i => new ProductIngredientDto(i.Id, i.Name, i.IsRemovable)).ToList()));
}
