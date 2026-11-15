using Microsoft.AspNetCore.SignalR.Client;
using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.Services
{
    public enum HubState { Disconnected, Connecting, Connected, Reconnecting }

    /// <summary>
    /// Conexão SignalR com /hubs/orders (painel da cozinha).
    /// Recebe "NewOrder" e "OrderUpdated" do servidor e confirma a exibição com AcknowledgeOrder.
    /// Os eventos chegam em thread de fundo: quem assina deve voltar para a MainThread antes de mexer na UI.
    /// </summary>
    public sealed class OrderHubClient : IAsyncDisposable
    {
        // Precisam coincidir com OrderHub no Menuz.Api.
        private const string NewOrderEvent = "NewOrder";
        private const string OrderUpdatedEvent = "OrderUpdated";

        private readonly HubConnection _connection;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public OrderHubClient()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(new Uri(new Uri(ApiConfig.BaseUrl), "hubs/orders"))
                .WithAutomaticReconnect(new RetryForever())
                .Build();

            _connection.On<OrderDto>(NewOrderEvent, dto => NewOrder?.Invoke(dto));
            _connection.On<OrderDto>(OrderUpdatedEvent, dto => OrderUpdated?.Invoke(dto));

            _connection.Reconnecting += _ => SetState(HubState.Reconnecting);
            _connection.Closed += _ => SetState(HubState.Disconnected);
            _connection.Reconnected += async _ =>
            {
                // Uma reconexão gera outro ConnectionId: é preciso entrar no grupo de novo.
                await _connection.InvokeAsync("JoinKitchen");
                SetState(HubState.Connected);
                Reconnected?.Invoke();
            };
        }

        public event Action<OrderDto>? NewOrder;
        public event Action<OrderDto>? OrderUpdated;

        /// <summary>Disparado após uma queda de conexão: o painel deve sincronizar para não perder pedidos.</summary>
        public event Action? Reconnected;

        public event Action<HubState>? StateChanged;

        public HubState State { get; private set; } = HubState.Disconnected;

        /// <summary>Conecta e entra no grupo da cozinha. Lança exceção se o servidor estiver fora do ar.</summary>
        public async Task StartKitchenAsync(CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct);
            try
            {
                if (_connection.State == HubConnectionState.Disconnected)
                {
                    SetState(HubState.Connecting);
                    try
                    {
                        await _connection.StartAsync(ct);
                    }
                    catch
                    {
                        SetState(HubState.Disconnected);
                        throw;
                    }
                }

                await _connection.InvokeAsync("JoinKitchen", ct);
                SetState(HubState.Connected);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task StopAsync()
        {
            await _gate.WaitAsync();
            try
            {
                if (_connection.State != HubConnectionState.Disconnected)
                    await _connection.StopAsync();
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>Informa ao servidor que o pedido apareceu na tela (medição do REQ. 5). Falhas são ignoradas.</summary>
        public async Task AcknowledgeAsync(int orderId)
        {
            if (_connection.State != HubConnectionState.Connected) return;
            try
            {
                await _connection.InvokeAsync("AcknowledgeOrder", orderId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Falha ao confirmar pedido {orderId}: {ex.Message}");
            }
        }

        public ValueTask DisposeAsync() => _connection.DisposeAsync();

        private Task SetState(HubState state)
        {
            State = state;
            StateChanged?.Invoke(state);
            return Task.CompletedTask;
        }

        /// <summary>Tenta reconectar para sempre: 0 s, 2 s, 5 s e depois a cada 10 s.</summary>
        private sealed class RetryForever : IRetryPolicy
        {
            private static readonly TimeSpan[] Delays = { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) };

            public TimeSpan? NextRetryDelay(RetryContext context) =>
                context.PreviousRetryCount < Delays.Length ? Delays[context.PreviousRetryCount] : TimeSpan.FromSeconds(10);
        }
    }
}
