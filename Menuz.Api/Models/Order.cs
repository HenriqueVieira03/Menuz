namespace Menuz.Api.Models;

public enum OrderStatus
{
    Pending = 0,    // recebido
    Preparing = 1,  // em preparo
    Ready = 2,      // pronto
    Delivered = 3,  // entregue (sai do painel)
    Cancelled = 4
}

public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Total { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

/// <summary>Item do pedido com "foto" do nome e do preço no momento da compra.</summary>
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}