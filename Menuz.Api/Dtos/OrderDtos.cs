using Menuz.Api.Models;

namespace Menuz.Api.Dtos;

public record CreateOrderItemRequest(int ProductId, int Quantity);
public record CreateOrderRequest(List<CreateOrderItemRequest> Items);
public record UpdateOrderStatusRequest(OrderStatus Status);

public record OrderItemDto(int Id, int ProductId, string ProductName, decimal UnitPrice, int Quantity);
public record OrderDto(int Id, DateTime CreatedAt, OrderStatus Status, decimal Total, List<OrderItemDto> Items);

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