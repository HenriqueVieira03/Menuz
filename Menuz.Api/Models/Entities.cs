namespace Menuz.Api.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public List<Product> Products { get; set; } = new();
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; } = true;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    // Variações reais do item (tamanhos, complementos e remoções)
    public List<ProductSize> Sizes { get; set; } = new();
    public List<ProductAddon> Addons { get; set; } = new();
    public List<ProductIngredient> Ingredients { get; set; } = new();
}

/// <summary>Tamanho do produto. Preço final = BasePrice + PriceDelta.</summary>
public class ProductSize
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceDelta { get; set; }
}

/// <summary>Complemento opcional com custo adicional (ex.: bacon extra).</summary>
public class ProductAddon
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

/// <summary>Ingrediente do produto; quando IsRemovable = true o cliente pode pedir sem.</summary>
public class ProductIngredient
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsRemovable { get; set; } = true;
}
