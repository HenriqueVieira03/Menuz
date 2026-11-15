using Menuz.Api.Models;

namespace Menuz.Api.Dtos;

public record CreateOrderItemRequest(int ProductId, int Quantity);
public record CreateOrderRequest(List<CreateOrderItemRequest> Items, DateTime? ClientSentAt = null);
public record UpdateOrderStatusRequest(OrderStatus Status);

public record OrderItemDto(int Id, int ProductId, string ProductName, decimal UnitPrice, int Quantity);
public record OrderDto(int Id, DateTime CreatedAt, OrderStatus Status, decimal Total, List<OrderItemDto> Items);

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
        DateTime.SpecifyKind(o.CreatedAt, DateTimeKind.Utc),
        o.Status,
        o.Total,
        o.Items.OrderBy(i => i.Id)
               .Select(i => new OrderItemDto(i.Id, i.ProductId, i.ProductName, i.UnitPrice, i.Quantity))
               .ToList());
}