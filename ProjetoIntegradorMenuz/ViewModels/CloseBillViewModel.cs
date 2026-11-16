using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoIntegradorMenuz.Models;
using ProjetoIntegradorMenuz.Services;

namespace ProjetoIntegradorMenuz.ViewModels
{
    /// <summary>Um pedido no resumo da conta (só os não cancelados).</summary>
    public class BillOrderSummaryViewModel
    {
        public BillOrderSummaryViewModel(OrderDto dto)
        {
            Id = dto.Id;
            Total = dto.Total;
            Time = dto.CreatedAt.ToLocalTime();
            Lines = dto.Items.Select(OrderLineViewModel.From).ToList();
        }

        public int Id { get; }
        public decimal Total { get; }
        public DateTime Time { get; }
        public IReadOnlyList<OrderLineViewModel> Lines { get; }

        public string Title => $"Pedido #{Id}";
        public string Subtitle => $"Enviado às {Time:HH:mm}";
    }

    /// <summary>
    /// Resumo da conta e pagamento (simulado). O botão de pagar só habilita quando a cozinha
    /// já terminou todos os pedidos; a tela se atualiza sozinha enquanto está aberta.
    /// </summary>
    public partial class CloseBillViewModel : ObservableObject
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(4);

        private readonly MenuApiService _api;
        private CancellationTokenSource? _cts;

        public CloseBillViewModel(MenuApiService api, TableService table)
        {
            _api = api;
            Table = table;
        }

        public TableService Table { get; }

        /// <summary>Pedidos cobrados (sem os cancelados), do mais antigo para o mais novo.</summary>
        public ObservableCollection<BillOrderSummaryViewModel> Orders { get; } = new();

        [ObservableProperty]
        private bool isBusy;

        // Só vira true depois da primeira resposta, para a tela não piscar "sem conta" ao abrir.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEmpty))]
        private bool isLoaded;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEmpty))]
        [NotifyPropertyChangedFor(nameof(CanPay))]
        private bool hasBill;

        [ObservableProperty]
        private decimal total;

        /// <summary>Pedidos Novos ou Em preparo: enquanto houver, a conta não pode ser fechada.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPendingOrders))]
        [NotifyPropertyChangedFor(nameof(BlockedText))]
        [NotifyPropertyChangedFor(nameof(CanPay))]
        private int pendingCount;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanPay))]
        [NotifyPropertyChangedFor(nameof(PayButtonText))]
        private bool isPaying;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? errorMessage;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNotice))]
        private string? noticeText;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool HasNotice => !string.IsNullOrEmpty(NoticeText);
        public bool IsEmpty => IsLoaded && !HasBill;
        public bool HasPendingOrders => PendingCount > 0;
        public bool CanPay => HasBill && PendingCount == 0 && !IsPaying;

        public string HeaderText => Table.TableNumber is { } n ? $"Mesa {n}" : "Mesa não definida";
        public string PayButtonText => IsPaying ? "Processando pagamento…" : "Realizar pagamento";

        public string BlockedText => PendingCount switch
        {
            0 => string.Empty,
            1 => "Ainda há 1 pedido em preparo. Assim que a cozinha terminar, você poderá pagar.",
            _ => $"Ainda há {PendingCount} pedidos em preparo. Assim que a cozinha terminar, você poderá pagar."
        };

        /// <summary>Chamado ao abrir a tela: carrega a conta e passa a atualizar a cada poucos segundos.</summary>
        public async Task StartAsync()
        {
            Stop();
            NoticeText = null;
            OnPropertyChanged(nameof(HeaderText));

            var cts = new CancellationTokenSource();
            _cts = cts;
            var ct = cts.Token;

            IsBusy = !IsLoaded;
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

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>Recarrega a conta agora (ex.: depois de o servidor recusar o pagamento).</summary>
        public Task RefreshAsync() => LoadAsync(_cts?.Token ?? CancellationToken.None);

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
                Apply(null);
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
                IsLoaded = true;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                if (ct.IsCancellationRequested) return;
                ErrorMessage = "Sem conexão com o servidor. Tentando novamente…";
            }
        }

        private void Apply(BillDto? bill)
        {
            if (bill is null)
            {
                Orders.Clear();
                Total = 0;
                PendingCount = 0;
                HasBill = false;
                return;
            }

            HasBill = true;
            PendingCount = bill.Orders.Count(o => o.Status is OrderStatus.Pending or OrderStatus.Preparing);

            // Só recria os cards quando a lista de pedidos cobrados muda (pedido novo ou cancelado).
            var billable = bill.Orders.Where(o => o.Status != OrderStatus.Cancelled).OrderBy(o => o.CreatedAt).ToList();
            var same = billable.Count == Orders.Count
                       && billable.Select(o => o.Id).SequenceEqual(Orders.Select(o => o.Id));
            if (!same)
            {
                Orders.Clear();
                foreach (var o in billable)
                    Orders.Add(new BillOrderSummaryViewModel(o));
            }

            if (IsLoaded && bill.Total != Total)
                NoticeText = "A conta foi atualizada. Confira o novo total antes de pagar.";

            Total = bill.Total;
        }

        /// <summary>
        /// Pagamento simulado: espera um instante (como se estivesse processando) e pede ao servidor para fechar a conta.
        /// Devolve a conta já paga, ou uma mensagem para mostrar ao cliente.
        /// </summary>
        public async Task<(BillDto? Bill, string? Error)> PayAsync()
        {
            if (!CanPay || Table.TableNumber is not { } table)
                return (null, "A conta ainda não pode ser fechada.");

            IsPaying = true;
            NoticeText = null;
            try
            {
                await Task.Delay(1200); // simulação do processamento do pagamento
                return await _api.CloseBillAsync(table, Total);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return (null, "Não foi possível falar com o servidor. Tente de novo.");
            }
            finally
            {
                IsPaying = false;
            }
        }

        [RelayCommand]
        private async Task GoBackAsync() => await Shell.Current.GoToAsync("..");
    }
}
