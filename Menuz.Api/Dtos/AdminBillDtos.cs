namespace Menuz.Api.Dtos;

/// <summary>Conta aberta de uma mesa, para a visão "Mesas abertas" da gerência. Total e contagens ignoram pedidos cancelados.</summary>
public record OpenBillDto(
    int Id,
    int TableNumber,
    DateTime OpenedAt,
    decimal Total,
    int OrdersCount,
    int InProgressCount);
