namespace Menuz.Api.Models;

public enum BillStatus
{
    Open = 0,  // mesa consumindo
    Paid = 1   // conta fechada (pagamento simulado)
}

/// <summary>
/// Conta de uma mesa: agrupa todos os pedidos feitos desde que a mesa começou a consumir até o fechamento.
/// É aberta automaticamente no primeiro pedido; depois de paga, o próximo pedido da mesa abre outra conta.
/// </summary>
public class Bill
{
    public int Id { get; set; }

    public int TableNumber { get; set; }

    public BillStatus Status { get; set; } = BillStatus.Open;

    /// <summary>Quando a conta foi aberta (primeiro pedido), relógio do servidor, UTC.</summary>
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Quando foi paga, UTC. Nulo enquanto estiver aberta.</summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>Total cobrado no fechamento (pedidos cancelados não entram). Nulo enquanto estiver aberta.</summary>
    public decimal? TotalPaid { get; set; }

    public List<Order> Orders { get; set; } = new();
}
