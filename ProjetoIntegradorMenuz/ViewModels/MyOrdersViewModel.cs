using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoIntegradorMenuz.Models;
using ProjetoIntegradorMenuz.Services;
using ProjetoIntegradorMenuz.Views;

namespace ProjetoIntegradorMenuz.ViewModels
{
    /// <summary>Um pedido na tela "Meus pedidos", com textos pensados para o cliente.</summary>
    public partial class ClientOrderViewModel : ObservableObject
    {
        public ClientOrderViewModel(OrderDto dto)
        {
            Id = dto.Id;
            CreatedAt = dto.CreatedAt;
            Total = dto.Total;
            Lines = dto.Items.Select(OrderLineViewModel.From).ToList();
            Status = dto.Status;
        }

        public int Id { get; }
        public DateTime CreatedAt { get; }
        public decimal Total { get; }
        public IReadOnlyList<OrderLineViewModel> Lines { get; }

        public string Title => $"Pedido #{Id}";
        public string Subtitle => $"Enviado às {CreatedAt.ToLocalTime():HH:mm}";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText))]
        [NotifyPropertyChangedFor(nameof(StatusHint))]
        [NotifyPropertyChangedFor(nameof(IsPreparing))]
        [NotifyPropertyChangedFor(nameof(IsReady))]
        [NotifyPropertyChangedFor(nameof(IsDelivered))]
        [NotifyPropertyChangedFor(nameof(IsCancelled))]
        private OrderStatus status;

        public bool IsPreparing => Status == OrderStatus.Preparing;
        public bool IsReady => Status == OrderStatus.Ready;
        public bool IsDelivered => Status == OrderStatus.Delivered;
        public bool IsCancelled => Status == OrderStatus.Cancelled;

        public string StatusText => Status switch
        {
            OrderStatus.Pending => "Recebido",
            OrderStatus.Preparing => "Em preparo",
            OrderStatus.Ready => "Pronto",
            OrderStatus.Delivered => "Entregue",
            _ => "Cancelado"
        };

        public string StatusHint => Status switch
        {
            OrderStatus.Pending => "A cozinha recebeu seu pedido.",
            OrderStatus.Preparing => "Seu pedido está sendo preparado.",
            OrderStatus.Ready => "Seu pedido está pronto e a caminho da mesa.",
            OrderStatus.Delivered => "Pedido entregue. Bom apetite!",
            _ => "Este pedido foi cancelado e não entra na conta."
        };

        public void Update(OrderDto dto) => Status = dto.Status;
    }

    /// <summary>
    /// "Meus pedidos": a conta aberta da mesa deste tablet, com o status de cada pedido.
    /// Enquanto a tela está aberta, consulta o servidor a cada poucos segundos (polling).
    /// </summary>
    public partial class MyOrdersViewModel : ObservableObject
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(4);

        private readonly MenuApiService _api;
        private CancellationTokenSource? _cts;

        public MyOrdersViewModel(MenuApiService api, TableService table)
        {
            _api = api;
            Table = table;
            Orders.CollectionChanged += OnOrdersChanged;
        }

        public TableService Table { get; }

        /// <summary>Pedidos da conta, do mais novo para o mais antigo.</summary>
        public ObservableCollection<ClientOrderViewModel> Orders { get; } = new();

        [ObservableProperty]
        private bool isBusy;

        // Só vira true depois da primeira resposta, para a tela não piscar "nenhum pedido" ao abrir.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEmpty))]
        private bool isLoaded;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? errorMessage;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HeaderText))]
        private string lastUpdateText = string.Empty;

        [ObservableProperty]
        private decimal total;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool HasOrders => Orders.Count > 0;
        public bool IsEmpty => IsLoaded && Orders.Count == 0;

        public string OrdersCountText => Orders.Count == 1 ? "1 pedido" : $"{Orders.Count} pedidos";

        public string HeaderText => Table.TableNumber is { } n
            ? (LastUpdateText.Length > 0 ? $"Mesa {n} · {LastUpdateText}" : $"Mesa {n}")
            : "Mesa não definida";

        public string EmptyText => Table.IsConfigured
            ? "Quando você fizer um pedido, ele aparece aqui e o status é atualizado automaticamente."
            : "Este tablet ainda não tem uma mesa definida. Volte ao cardápio e toque na mesa para configurar.";

        private void OnOrdersChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasOrders));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(OrdersCountText));
        }

        /// <summary>Chamado ao abrir a tela: carrega a conta e passa a atualizar a cada poucos segundos.</summary>
        public async Task StartAsync()
        {
            Stop();
            OnPropertyChanged(nameof(HeaderText));
            OnPropertyChanged(nameof(EmptyText));

            var cts = new CancellationTokenSource();
            _cts = cts;
            var ct = cts.Token;

            IsBusy = Orders.Count == 0;
            try
            {
                await LoadAsync(ct);
            }
            finally
            {
                IsBusy = false;
            }

            if (!ct.IsCancellationRequested)
                _ = PollAsync(ct);
        }

        /// <summary>Chamado ao sair da tela: para de consultar o servidor.</summary>
        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async Task PollAsync(CancellationToken ct)
        {
            try
            {
                using var timer = new PeriodicTimer(PollInterval);
                while (await timer.WaitForNextTickAsync(ct))
                    await LoadAsync(ct);
            }
            catch (OperationCanceledException)
            {
                // Tela fechada: fim normal do polling.
            }
        }

        private async Task LoadAsync(CancellationToken ct)
        {
            if (Table.TableNumber is not { } table)
            {
                Orders.Clear();
                Total = 0;
                ErrorMessage = null;
                IsLoaded = true;
                return;
            }

            try
            {
                var bill = await _api.GetOpenBillAsync(table, ct);
                if (ct.IsCancellationRequested) return;

                ErrorMessage = null;
                Apply(bill);
                LastUpdateText = $"atualizado às {DateTime.Now:HH:mm:ss}";
                IsLoaded = true;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                if (ct.IsCancellationRequested) return;
                ErrorMessage = "Sem conexão com o servidor. Tentando novamente…";
            }
        }

        /// <summary>Reconcilia a lista com a conta do servidor, sem recriar os cards que já existem.</summary>
        private void Apply(BillDto? bill)
        {
            if (bill is null)
            {
                Orders.Clear();
                Total = 0;
                return;
            }

            var ids = bill.Orders.Select(o => o.Id).ToHashSet();
            for (var i = Orders.Count - 1; i >= 0; i--)
                if (!ids.Contains(Orders[i].Id))
                    Orders.RemoveAt(i);

            foreach (var dto in bill.Orders)
            {
                var existing = Orders.FirstOrDefault(o => o.Id == dto.Id);
                if (existing is not null)
                {
                    existing.Update(dto);
                    continue;
                }

                // Mais novo primeiro: entra depois de todos os pedidos mais recentes que ele.
                var index = 0;
                while (index < Orders.Count && Orders[index].CreatedAt > dto.CreatedAt)
                    index++;

                Orders.Insert(index, new ClientOrderViewModel(dto));
            }

            Total = bill.Total;
        }

        [RelayCommand]
        private async Task RefreshAsync() => await LoadAsync(_cts?.Token ?? CancellationToken.None);

        [RelayCommand]
        private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");

        /// <summary>Abre o resumo da conta, de onde o cliente faz o pagamento (simulado).</summary>
        [RelayCommand]
        private async Task CloseBillAsync() => await Shell.Current.GoToAsync(nameof(CloseBillPage));
    }
}
