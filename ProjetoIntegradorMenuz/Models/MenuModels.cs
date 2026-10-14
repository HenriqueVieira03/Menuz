namespace ProjetoIntegradorMenuz.Models
{
    // Espelham os DTOs retornados pela Web API (GET /api/menu).

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
        public int CategoryId { get; set; }
        public List<ProductSize> Sizes { get; set; } = new();
        public List<ProductAddon> Addons { get; set; } = new();
        public List<ProductIngredient> Ingredients { get; set; } = new();
    }

    public class ProductSize
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal PriceDelta { get; set; }
    }

    public class ProductAddon
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class ProductIngredient
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsRemovable { get; set; }
    }
}
