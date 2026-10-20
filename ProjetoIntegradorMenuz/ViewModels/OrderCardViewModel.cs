using CommunityToolkit.Mvvm.ComponentModel;
using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.ViewModels
{
    public record OrderLineViewModel(string Text);

    /// <summary>Card de um pedido no painel da cozinha.</summary>
    public partial class OrderCardViewModel : ObservableObject
    {
        public OrderCardViewModel(OrderDto dto)
        {
            Id = dto.Id;
            CreatedAt = dto.CreatedAt;
            Total = dto.Total;
            Lines = dto.Items.Select(i => new OrderLineViewModel($"{i.Quantity}x {i.ProductName}")).ToList();
            Status = dto.Status;
        }

        public int Id { get; }
        public DateTime CreatedAt { get; }
        public decimal Total { get; }
        public IReadOnlyList<OrderLineViewModel> Lines { get; }

        public string Title => $"Pedido #{Id}";
        public string TimeText => CreatedAt.ToLocalTime().ToString("HH:mm");

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        [NotifyPropertyChangedFor(nameof(IsPreparing))]
        [NotifyPropertyChangedFor(nameof(IsReady))]
        [NotifyPropertyChangedFor(nameof(ActionText))]
        [NotifyPropertyChangedFor(nameof(NextStatus))]
        private OrderStatus status;

        public bool IsPreparing => Status == OrderStatus.Preparing;
        public bool IsReady => Status == OrderStatus.Ready;

        public string StatusText => Status switch
        {
            OrderStatus.Pending => "Novo",
            OrderStatus.Preparing => "Em preparo",
            OrderStatus.Ready => "Pronto",
            OrderStatus.Delivered => "Entregue",
            _ => "Cancelado"
        };

        public string ActionText => Status switch
        {
            OrderStatus.Pending => "Iniciar preparo",
            OrderStatus.Preparing => "Marcar como pronto",
            _ => "Entregar"
        };

        public OrderStatus NextStatus => Status switch
        {
            OrderStatus.Pending => OrderStatus.Preparing,
            OrderStatus.Preparing => OrderStatus.Ready,
            _ => OrderStatus.Delivered
        };

        public void Update(OrderDto dto) => Status = dto.Status;
    }
}