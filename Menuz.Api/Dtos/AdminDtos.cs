namespace Menuz.Api.Dtos;

// ---- Cardápio (gerência) ----

public record CategoryAdminDto(int Id, string Name, int DisplayOrder, int ProductCount);

public record SaveCategoryRequest(string Name, int DisplayOrder);

/// <summary>Produto completo para edição, inclusive os indisponíveis.</summary>
public record ProductAdminDto(
    int Id,
    string Name,
    string Description,
    decimal BasePrice,
    string? ImageUrl,
    bool IsAvailable,
    int CategoryId,
    string CategoryName,
    List<ProductSizeDto> Sizes,
    List<ProductAddonDto> Addons,
    List<ProductIngredientDto> Ingredients);

// Nas opções, Id = null cria uma nova; Id preenchido atualiza a existente (mantém o id que os tablets já conhecem).
public record SaveSizeRequest(int? Id, string Name, decimal PriceDelta);
public record SaveAddonRequest(int? Id, string Name, decimal Price);
public record SaveIngredientRequest(int? Id, string Name, bool IsRemovable);

public record SaveProductRequest(
    string Name,
    string? Description,
    decimal BasePrice,
    string? ImageUrl,
    bool IsAvailable,
    int CategoryId,
    List<SaveSizeRequest>? Sizes,
    List<SaveAddonRequest>? Addons,
    List<SaveIngredientRequest>? Ingredients);

public record SetAvailabilityRequest(bool IsAvailable);

// ---- Indicadores (gerência) ----

public record TopProductDto(string Name, int Quantity, decimal Revenue);

/// <summary>Resumo do período para o painel da gerência. Valores monetários excluem pedidos cancelados.</summary>
public record DashboardDto(
    DateTime From,
    DateTime To,
    int OrdersCount,
    int CancelledCount,
    int OpenCount,
    decimal Revenue,
    decimal AverageTicket,
    double? AverageMinutesToReady,
    double? AverageMinutesToDeliver,
    List<TopProductDto> TopProducts,
    LatencyStatsDto KitchenLatency,
    int BillsClosedCount = 0,
    decimal AverageBillTotal = 0m);

public record ImageUploadDto(string Path);
