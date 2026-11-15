namespace Menuz.Api.Models;

public enum OrderStatus
{
    Pending = 0,    // recebido
    Preparing = 1,  // em preparo
    Ready = 2,      // pronto
    Delivered = 3,  // entregue (sai do painel)
    Cancelled = 4
}

public static class OrderRules
{
    public const int MaxTableNumber = 999;
    public const int NotesMaxLength = 200;
    public const int MaxQuantityPerItem = 50;
}

public class Order
{
    public int Id { get; set; }

    /// <summary>Momento em que o servidor recebeu e gravou o pedido (relógio do servidor, UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Medição de tempo de resposta (REQ. 5)

    /// <summary>Momento do toque em "Finalizar pedido" no tablet (relógio do tablet, UTC).</summary>
    public DateTime? ClientSentAt { get; set; }

    /// <summary>Momento em que a primeira tela da cozinha confirmou que exibiu o pedido (relógio do servidor, UTC).</summary>
    public DateTime? KitchenReceivedAt { get; set; }

    /// <summary>Mesa de onde o pedido foi feito (configurada no tablet).</summary>
    public int TableNumber { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    // Horários de cada mudança de status (relógio do servidor, UTC): base dos indicadores do PDV.
    public DateTime? PreparingAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public decimal Total { get; set; }
    public List<OrderItem> Items { get; set; } = new();

    /// <summary>Muda o status e registra o horário da primeira vez que o pedido chegou nele.</summary>
    public void SetStatus(OrderStatus status, DateTime now)
    {
        Status = status;
        switch (status)
        {
            case OrderStatus.Preparing: PreparingAt ??= now; break;
            case OrderStatus.Ready: ReadyAt ??= now; break;
            case OrderStatus.Delivered: DeliveredAt ??= now; break;
            case OrderStatus.Cancelled: CancelledAt ??= now; break;
        }
    }
}

/// <summary>
/// Item do pedido com "foto" do nome, das escolhas e do preço no momento da compra.
/// Guardar os nomes (e não só os ids) mantém o histórico correto mesmo se o cardápio mudar depois.
/// </summary>
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Tamanho escolhido (null quando o produto não tem tamanhos).</summary>
    public string? SizeName { get; set; }

    /// <summary>Nomes dos adicionais escolhidos.</summary>
    public List<string> Addons { get; set; } = new();

    /// <summary>Nomes dos ingredientes que o cliente pediu para retirar.</summary>
    public List<string> RemovedIngredients { get; set; } = new();

    /// <summary>Observação livre do cliente (ex.: "bem passado").</summary>
    public string? Notes { get; set; }

    /// <summary>Preço unitário final = preço base + acréscimo do tamanho + adicionais.</summary>
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}