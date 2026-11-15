namespace ProjetoIntegradorMenuz.Models
{
    // Espelham os DTOs de /api/orders. Os valores numéricos do enum devem coincidir com os da API.
    public enum OrderStatus { Pending = 0, Preparing = 1, Ready = 2, Delivered = 3, Cancelled = 4 }

    public class CreateOrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int? SizeId { get; set; }
        public List<int> AddonIds { get; set; } = new();
        public List<int> RemovedIngredientIds { get; set; } = new();
        public string? Notes { get; set; }
    }

    public class CreateOrderRequest
    {
        public int TableNumber { get; set; }

        public List<CreateOrderItemRequest> Items { get; set; } = new();

        /// <summary>Momento do toque em "Finalizar pedido" (UTC), usado na medição de tempo de resposta.</summary>
        public DateTime ClientSentAt { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        public OrderStatus Status { get; set; }
    }

    public class OrderItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? SizeName { get; set; }
        public List<string> Addons { get; set; } = new();
        public List<string> RemovedIngredients { get; set; } = new();
        public string? Notes { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public int TableNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public OrderStatus Status { get; set; }
        public decimal Total { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }
}
