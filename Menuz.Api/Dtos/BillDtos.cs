using Menuz.Api.Models;

namespace Menuz.Api.Dtos;

/// <summary>Conta da mesa com seus pedidos. Total exclui pedidos cancelados.</summary>
public record BillDto(
    int Id,
    int TableNumber,
    DateTime OpenedAt,
    BillStatus Status,
    decimal Total,
    List<OrderDto> Orders,
    DateTime? ClosedAt = null,
    decimal? TotalPaid = null);

/// <summary>Fechamento da conta: o tablet envia o total que o cliente viu no resumo.</summary>
public record CloseBillRequest(decimal ExpectedTotal);

public static class BillMappings
{
    public static BillDto ToDto(this Bill b) => new(
        b.Id,
        b.TableNumber,
        DateTime.SpecifyKind(b.OpenedAt, DateTimeKind.Utc),
        b.Status,
        b.Orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total),
        b.Orders.OrderBy(o => o.CreatedAt).Select(o => o.ToDto()).ToList(),
        b.ClosedAt is { } closed ? DateTime.SpecifyKind(closed, DateTimeKind.Utc) : null,
        b.TotalPaid);
}
