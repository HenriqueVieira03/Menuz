using CommunityToolkit.Mvvm.ComponentModel;
using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.ViewModels
{
    /// <summary>Card de uma mesa com conta aberta na aba "Mesas abertas" da gerência.</summary>
    public partial class OpenBillViewModel : ObservableObject
    {
        public OpenBillViewModel(OpenBill dto)
        {
            Id = dto.Id;
            TableNumber = dto.TableNumber;
            OpenedAt = dto.OpenedAt;
            Update(dto);
        }

        public int Id { get; }
        public int TableNumber { get; }
        public DateTime OpenedAt { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalText))]
        private decimal total;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Subtitle))]
        private int ordersCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasInProgress))]
        [NotifyPropertyChangedFor(nameof(CanClose))]
        [NotifyPropertyChangedFor(nameof(InProgressText))]
        [NotifyPropertyChangedFor(nameof(InProgressNotice))]
        private int inProgressCount;

        public string Title => $"Mesa {TableNumber}";

        public string Subtitle =>
            $"Aberta às {OpenedAt.ToLocalTime():HH:mm} · {OrdersCount} {(OrdersCount == 1 ? "pedido" : "pedidos")}";

        public string TotalText => Total.ToString("C2");

        public bool HasInProgress => InProgressCount > 0;

        /// <summary>A API só encerra contas sem pedidos Novos ou Em preparo.</summary>
        public bool CanClose => InProgressCount == 0;

        /// <summary>Aviso para o cartão da mesa: vazio quando não há pedido em andamento (o aviso some).</summary>
        public string InProgressNotice => HasInProgress ? InProgressText : string.Empty;

        public string InProgressText => InProgressCount == 1
            ? "1 pedido em andamento. Conclua ou cancele antes de encerrar."
            : $"{InProgressCount} pedidos em andamento. Conclua ou cancele antes de encerrar.";

        public void Update(OpenBill dto)
        {
            Total = dto.Total;
            OrdersCount = dto.OrdersCount;
            InProgressCount = dto.InProgressCount;
        }
    }
}
