namespace ProjetoIntegradorMenuz.Models
{
    // Espelham os DTOs de /api/auth e /api/admin.

    public static class StaffRoles
    {
        public const string Kitchen = "Kitchen";
        public const string Manager = "Manager";
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class CategoryAdmin
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public int ProductCount { get; set; }
    }

    public class SaveCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class ProductAdmin
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<ProductSize> Sizes { get; set; } = new();
        public List<ProductAddon> Addons { get; set; } = new();
        public List<ProductIngredient> Ingredients { get; set; } = new();
    }

    public class SaveSizeRequest
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal PriceDelta { get; set; }
    }

    public class SaveAddonRequest
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class SaveIngredientRequest
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsRemovable { get; set; }
    }

    public class SaveProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public int CategoryId { get; set; }
        public List<SaveSizeRequest> Sizes { get; set; } = new();
        public List<SaveAddonRequest> Addons { get; set; } = new();
        public List<SaveIngredientRequest> Ingredients { get; set; } = new();
    }

    public class TopProduct
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }

    public class LatencyStats
    {
        public int Samples { get; set; }
        public double AverageMs { get; set; }
        public double MedianMs { get; set; }
        public double P95Ms { get; set; }
        public double MaxMs { get; set; }
        public double PercentUnder2s { get; set; }
    }

    public class Dashboard
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int OrdersCount { get; set; }
        public int CancelledCount { get; set; }
        public int OpenCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal AverageTicket { get; set; }
        public double? AverageMinutesToReady { get; set; }
        public double? AverageMinutesToDeliver { get; set; }
        public List<TopProduct> TopProducts { get; set; } = new();
        public LatencyStats KitchenLatency { get; set; } = new();
    }
}
