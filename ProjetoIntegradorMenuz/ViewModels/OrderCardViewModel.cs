using CommunityToolkit.Mvvm.ComponentModel;
using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.ViewModels
{
    /// <summary>Linha do card: "2x X-Burger (Duplo)" e, abaixo, as escolhas ("+ Bacon · sem Cebola · Obs: ...").</summary>
    public record OrderLineViewModel(string Text, string Details)
    {
        public bool HasDetails => Details.Length > 0;

        public static OrderLineViewModel From(OrderItemDto i) => new(
            i.SizeName is null ? $"{i.Quantity}x {i.ProductName}" : $"{i.Quantity}x {i.ProductName} ({i.SizeName})",
            CartItem.FormatOptions(i.Addons, i.RemovedIngredients, i.Notes));
    }

    /// <summary>Card de um pedido no painel da cozinha.</summary>
    public partial class OrderCardViewModel : ObservableObject
    {
        public OrderCardViewModel(OrderDto dto)
        {
            Id = dto.Id;
            TableNumber = dto.TableNumber;
            CreatedAt = dto.CreatedAt;
            Total = dto.Total;
            Lines = dto.Items.Select(OrderLineViewModel.From).ToList();
            Status = dto.Status;
        }

        public int Id { get; }
        public int TableNumber { get; }
        public DateTime CreatedAt { get; }
        public decimal Total { get; }
        public IReadOnlyList<OrderLineViewModel> Lines { get; }

        // A mesa é o que a cozinha mais precisa ver; pedidos antigos (antes da mesa existir) vêm com 0.
        public string Title => TableNumber > 0 ? $"Mesa {TableNumber}" : "Mesa não informada";
        public string TimeText => CreatedAt.ToLocalTime().ToString("HH:mm");
        public string Subtitle => $"Pedido #{Id} · recebido às {TimeText}";

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