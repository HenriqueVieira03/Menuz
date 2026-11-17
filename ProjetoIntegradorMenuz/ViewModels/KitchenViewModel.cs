using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoIntegradorMenuz.Models;
using ProjetoIntegradorMenuz.Services;

namespace ProjetoIntegradorMenuz.ViewModels
{
    /// <summary>
    /// Painel da cozinha (KDS). Os pedidos chegam em tempo real pelo SignalR;
    /// a busca via REST (Sincronizar) carrega o estado inicial e cobre quedas de conexão.
    /// </summary>
    public partial class KitchenViewModel : ObservableObject
    {
        private readonly MenuApiService _api;
        private readonly OrderHubClient _hub;
        private bool _subscribed;

        public KitchenViewModel(MenuApiService api, OrderHubClient hub)
        {
            _api = api;
            _hub = hub;
        }

        public ObservableCollection<OrderCardViewModel> Orders { get; } = new();

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string lastSyncText = "Ainda não sincronizado";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ConnectionText))]
        [NotifyPropertyChangedFor(nameof(IsLive))]
        private HubState connectionState = HubState.Disconnected;

        public bool IsLive => ConnectionState == HubState.Connected;

        public string ConnectionText => ConnectionState switch
        {
            HubState.Connected => "Ao vivo",
            HubState.Connecting => "Conectando…",
            HubState.Reconnecting => "Reconectando…",
            _ => "Offline"
        };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? errorMessage;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>Chamado ao abrir a tela: conecta ao hub e carrega os pedidos em andamento.</summary>
        public async Task StartAsync()
        {
            if (!_subscribed)
            {
                _hub.NewOrder += OnNewOrder;
                _hub.OrderUpdated += OnOrderUpdated;
                _hub.Reconnected += OnReconnected;
                _hub.StateChanged += OnStateChanged;
                _subscribed = true;

                ConnectionState = _hub.State;
                try
                {
                    await _hub.AcquireAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SignalR indisponível: {ex.Message}");
                }
            }

            await ConnectAndSyncAsync();
        }

        /// <summary>Chamado ao sair da tela: desconecta para não receber eventos em uma página fechada.</summary>
        public async Task StopAsync()
        {
            if (_subscribed)
            {
                _hub.NewOrder -= OnNewOrder;
                _hub.OrderUpdated -= OnOrderUpdated;
                _hub.Reconnected -= OnReconnected;
                _hub.StateChanged -= OnStateChanged;
                _subscribed = false;
                await _hub.ReleaseAsync(); // o painel da gerência pode continuar usando a conexão
            }
        }

        /// <summary>
        /// Conecta primeiro e só depois busca a lista: assim nenhum pedido criado
        /// entre as duas etapas fica de fora (no pior caso chega duas vezes e é deduplicado pelo Id).
        /// </summary>
        [RelayCommand]
        private async Task SyncAsync() => await ConnectAndSyncAsync();

        private async Task ConnectAndSyncAsync()
        {
            if (_hub.State == HubState.Disconnected)
            {
                try
                {
                    await _hub.EnsureConnectedAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SignalR indisponível: {ex.Message}");
                }
            }

            await LoadOrdersAsync();

            if (_hub.State != HubState.Connected && !HasError)
                ErrorMessage = "Sem conexão em tempo real. Novos pedidos só aparecem ao sincronizar.";
        }

        /// <summary>Busca os pedidos na API e reconcilia a lista (sem recriar os cards existentes).</summary>
        private async Task LoadOrdersAsync()
        {
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var incoming = await _api.GetActiveOrdersAsync();
                var ids = incoming.Select(o => o.Id).ToHashSet();

                for (var i = Orders.Count - 1; i >= 0; i--)
                    if (!ids.Contains(Orders[i].Id))
                        Orders.RemoveAt(i);

                foreach (var dto in incoming) // a API devolve do mais antigo para o mais novo
                    Upsert(dto);

                LastSyncText = $"Sincronizado às {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                ErrorMessage = "Não foi possível sincronizar. Verifique se a API está em execução.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task AdvanceAsync(OrderCardViewModel? card)
        {
            if (card is null) return;
            ErrorMessage = null;

            try
            {
                var updated = await _api.UpdateOrderStatusAsync(card.Id, card.NextStatus);
                Apply(updated);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                ErrorMessage = "Não foi possível atualizar o pedido. Tente sincronizar novamente.";
            }
        }

        [RelayCommand]
        private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");

        // ---- Eventos do SignalR (chegam fora da thread de UI) ----

        private void OnNewOrder(OrderDto dto) =>
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Upsert(dto);
                LastSyncText = $"Último pedido às {DateTime.Now:HH:mm:ss}";

                // Confirma depois que o card já está na lista (medição de tempo do REQ. 5).
                _ = _hub.AcknowledgeAsync(dto.Id);
            });

        private void OnOrderUpdated(OrderDto dto) =>
            MainThread.BeginInvokeOnMainThread(() => Apply(dto));

        private void OnReconnected() =>
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                ErrorMessage = null;
                await LoadOrdersAsync(); // recupera pedidos enviados enquanto a conexão estava fora
            });

        private void OnStateChanged(HubState state) =>
            MainThread.BeginInvokeOnMainThread(() => ConnectionState = state);

        // ---- Lista ----

        /// <summary>Aplica um status vindo do servidor: entregue/cancelado sai do painel.</summary>
        private void Apply(OrderDto dto)
        {
            if (dto.Status is OrderStatus.Delivered or OrderStatus.Cancelled)
            {
                var card = Orders.FirstOrDefault(o => o.Id == dto.Id);
                if (card is not null) Orders.Remove(card);
            }
            else
            {
                Upsert(dto);
            }
        }

        /// <summary>Atualiza o card se já existir; senão insere mantendo a ordem de chegada.</summary>
        private void Upsert(OrderDto dto)
        {
            var existing = Orders.FirstOrDefault(o => o.Id == dto.Id);
            if (existing is not null)
            {
                existing.Update(dto);
                return;
            }

            var index = 0;
            while (index < Orders.Count && Orders[index].CreatedAt <= dto.CreatedAt)
                index++;

            Orders.Insert(index, new OrderCardViewModel(dto));
        }
    }
}
