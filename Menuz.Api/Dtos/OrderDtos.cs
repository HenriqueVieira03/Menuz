using Menuz.Api.Models;

namespace Menuz.Api.Dtos;

/// <summary>
/// Item enviado pelo tablet. As escolhas vão por id; nomes e preços são resolvidos no servidor.
/// SizeId é obrigatório quando o produto tem tamanhos.
/// </summary>
public record CreateOrderItemRequest(
    int ProductId,
    int Quantity,
    int? SizeId = null,
    List<int>? AddonIds = null,
    List<int>? RemovedIngredientIds = null,
    string? Notes = null);

public record CreateOrderRequest(int TableNumber, List<CreateOrderItemRequest> Items, DateTime? ClientSentAt = null);
public record UpdateOrderStatusRequest(OrderStatus Status);

public record OrderItemDto(
    int Id,
    int ProductId,
    string ProductName,
    string? SizeName,
    List<string> Addons,
    List<string> RemovedIngredients,
    string? Notes,
    decimal UnitPrice,
    int Quantity);

public record OrderDto(
    int Id, int TableNumber, DateTime CreatedAt, OrderStatus Status, decimal Total, List<OrderItemDto> Items, int? BillId = null);

/// <summary>Estatística descritiva dos tempos de entrega à cozinha, em milissegundos.</summary>
public record LatencyStatsDto(int Samples, double AverageMs, double MedianMs, double P95Ms, double MaxMs, double PercentUnder2s);

/// <summary>
/// Resumo para validar o critério de sucesso (&lt; 2 s).
/// ServerToKitchen usa só o relógio do servidor (sempre confiável).
/// EndToEnd vai do toque no tablet até a confirmação da cozinha e depende de os relógios do tablet e do servidor estarem sincronizados.
/// </summary>
public record OrderMetricsDto(int TotalOrders, int AcknowledgedOrders, LatencyStatsDto ServerToKitchen, LatencyStatsDto EndToEnd);

public static class OrderMappings
{
    public static OrderDto ToDto(this Order o) => new(
        o.Id,
        o.TableNumber,
        DateTime.SpecifyKind(o.CreatedAt, DateTimeKind.Utc),
        o.Status,
        o.Total,
        o.Items.OrderBy(i => i.Id)
               .Select(i => new OrderItemDto(
                   i.Id, i.ProductId, i.ProductName, i.SizeName,
                   i.Addons, i.RemovedIngredients, i.Notes, i.UnitPrice, i.Quantity))
               .ToList(),
        o.BillId);
}