using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoIntegradorMenuz.Models;
using ProjetoIntegradorMenuz.Services;
using ProjetoIntegradorMenuz.Views;

namespace ProjetoIntegradorMenuz.ViewModels
{
    public enum ManagerTab { Overview, Orders, Tables, Users, Menu }

    /// <summary>
    /// Painel da gerência (PDV): indicadores do dia, todos os pedidos do dia (com cancelamento)
    /// e gestão do cardápio. Pedidos e indicadores atualizam em tempo real pelo SignalR.
    /// </summary>
    public partial class ManagerViewModel : ObservableObject
    {
        private readonly AdminApiService _admin;
        private readonly AuthService _auth;
        private readonly OrderHubClient _hub;
        private List<ManagerProductViewModel> _allProducts = new();
        private bool _subscribed;
        private bool _dashboardLoading;
        private bool _billsLoading;
        private CancellationTokenSource? _pollCts;

        public ManagerViewModel(AdminApiService admin, AuthService auth, OrderHubClient hub, TableService table)
        {
            _admin = admin;
            _auth = auth;
            _hub = hub;
            Table = table;
        }

        public TableService Table { get; }
        public string UserText => $"Olá, {_auth.DisplayName}";

        // ---------------- Abas ----------------

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOverview))]
        [NotifyPropertyChangedFor(nameof(IsOrders))]
        [NotifyPropertyChangedFor(nameof(IsTables))]
        [NotifyPropertyChangedFor(nameof(IsUsers))]
        [NotifyPropertyChangedFor(nameof(IsMenu))]
        private ManagerTab selectedTab = ManagerTab.Overview;

        public bool IsOverview => SelectedTab == ManagerTab.Overview;
        public bool IsOrders => SelectedTab == ManagerTab.Orders;
        public bool IsTables => SelectedTab == ManagerTab.Tables;
        public bool IsUsers => SelectedTab == ManagerTab.Users;
        public bool IsMenu => SelectedTab == ManagerTab.Menu;

        // Ao abrir a aba de mesas, já mostra as contas como estão agora.
        partial void OnSelectedTabChanged(ManagerTab value)
        {
            if (value == ManagerTab.Tables) _ = ReloadOpenBillsAsync();
            if (value == ManagerTab.Users) _ = ReloadUsersAsync();
        }

        [RelayCommand]
        private void SelectTab(string tab)
        {
            if (Enum.TryParse<ManagerTab>(tab, out var t)) SelectedTab = t;
        }

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? errorMessage;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // ---------------- Visão geral ----------------

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OrdersText))]
        [NotifyPropertyChangedFor(nameof(RevenueText))]
        [NotifyPropertyChangedFor(nameof(TicketText))]
        [NotifyPropertyChangedFor(nameof(OpenText))]
        [NotifyPropertyChangedFor(nameof(CancelledText))]
        [NotifyPropertyChangedFor(nameof(BillsClosedText))]
        [NotifyPropertyChangedFor(nameof(BillTicketText))]
        [NotifyPropertyChangedFor(nameof(ReadyText))]
        [NotifyPropertyChangedFor(nameof(DeliverText))]
        [NotifyPropertyChangedFor(nameof(LatencyAvgText))]
        [NotifyPropertyChangedFor(nameof(LatencyP95Text))]
        [NotifyPropertyChangedFor(nameof(LatencyUnderText))]
        [NotifyPropertyChangedFor(nameof(LatencySamplesText))]
        [NotifyPropertyChangedFor(nameof(HasTopProducts))]
        [NotifyPropertyChangedFor(nameof(HasNoTopProducts))]
        private Dashboard? dashboard;

        public string OrdersText => Dashboard?.OrdersCount.ToString() ?? "—";
        public string RevenueText => Dashboard is null ? "—" : $"{Dashboard.Revenue:C2}";
        public string TicketText => Dashboard is null ? "—" : $"{Dashboard.AverageTicket:C2}";
        public string OpenText => Dashboard?.OpenCount.ToString() ?? "—";
        public string CancelledText => Dashboard?.CancelledCount.ToString() ?? "—";
        public string BillsClosedText => Dashboard?.BillsClosedCount.ToString() ?? "—";
        public string BillTicketText => Dashboard is null ? "—" : $"{Dashboard.AverageBillTotal:C2}";
        public string ReadyText => Minutes(Dashboard?.AverageMinutesToReady);
        public string DeliverText => Minutes(Dashboard?.AverageMinutesToDeliver);

        public string LatencyAvgText => Ms(Dashboard?.KitchenLatency, l => l.AverageMs);
        public string LatencyP95Text => Ms(Dashboard?.KitchenLatency, l => l.P95Ms);
        public string LatencyUnderText => Dashboard is { KitchenLatency.Samples: > 0 } d ? $"{d.KitchenLatency.PercentUnder2s:0.#}%" : "—";
        public string LatencySamplesText => $"{Dashboard?.KitchenLatency.Samples ?? 0} pedidos medidos hoje";

        public ObservableCollection<TopProductViewModel> TopProducts { get; } = new();
        public bool HasTopProducts => TopProducts.Count > 0;
        public bool HasNoTopProducts => TopProducts.Count == 0;

        // ---------------- Pedidos ----------------

        /// <summary>Pedidos de hoje, do mais novo para o mais antigo.</summary>
        public ObservableCollection<OrderCardViewModel> Orders { get; } = new();

        // ---------------- Usuários ----------------

        public ObservableCollection<ManagerUserViewModel> Users { get; } = new();
        public bool HasUsers => Users.Count > 0;
        public bool HasNoUsers => Users.Count == 0;

        public string UsersSummary => Users.Count == 0
            ? "Nenhum usuário cadastrado."
            : $"{Users.Count(u => u.IsActive)} ativos de {Users.Count} {(Users.Count == 1 ? "usuário" : "usuários")}";

        // ---------------- Mesas abertas ----------------

        /// <summary>Uma conta aberta por mesa, em ordem de mesa.</summary>
        public ObservableCollection<OpenBillViewModel> OpenBills { get; } = new();
        public bool HasOpenBills => OpenBills.Count > 0;
        public bool HasNoOpenBills => OpenBills.Count == 0;

        public string OpenBillsSummary => OpenBills.Count == 0
            ? "Nenhuma mesa com conta aberta."
            : $"{OpenBills.Count} {(OpenBills.Count == 1 ? "mesa" : "mesas")} com conta aberta · {OpenBills.Sum(b => b.Total):C2} em aberto";

        // ---------------- Cardápio ----------------

        public ObservableCollection<CategoryFilterViewModel> CategoryFilters { get; } = new();
        public ObservableCollection<ManagerProductViewModel> Products { get; } = new();
        private CategoryAdmin? _categoryFilter;

        // ---------------- Ciclo de vida ----------------

        public async Task StartAsync()
        {
            OnPropertyChanged(nameof(UserText));

            if (!_subscribed)
            {
                _hub.NewOrder += OnOrderEvent;
                _hub.OrderUpdated += OnOrderEvent;
                _hub.Reconnected += OnReconnected;
                _subscribed = true;
                try { await _hub.AcquireAsync(); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"SignalR indisponível: {ex.Message}"); }
            }

            await RefreshAsync();

            // Enquanto a aba "Mesas abertas" está visível, a lista se atualiza sozinha (cliente que paga pelo tablet,
            // cozinha que conclui pedidos...), sem depender de eventos do servidor.
            _pollCts?.Cancel();
            _pollCts?.Dispose();
            _pollCts = new CancellationTokenSource();
            _ = PollOpenBillsAsync(_pollCts.Token);
        }

        private async Task PollOpenBillsAsync(CancellationToken ct)
        {
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
                while (await timer.WaitForNextTickAsync(ct))
                {
                    if (IsTables) await ReloadOpenBillsAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Painel fechado: fim normal do polling.
            }
        }

        public async Task StopAsync()
        {
            _pollCts?.Cancel();
            _pollCts?.Dispose();
            _pollCts = null;

            if (!_subscribed) return;
            _hub.NewOrder -= OnOrderEvent;
            _hub.OrderUpdated -= OnOrderEvent;
            _hub.Reconnected -= OnReconnected;
            _subscribed = false;
            await _hub.ReleaseAsync();
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await RunAsync(async () =>
            {
                var (from, to) = Today();
                var dashboardTask = _admin.GetDashboardAsync(from, to);
                var ordersTask = _admin.GetOrdersAsync(from, to);
                var categoriesTask = _admin.GetCategoriesAsync();
                var productsTask = _admin.GetProductsAsync();
                var billsTask = _admin.GetOpenBillsAsync();
                await Task.WhenAll(dashboardTask, ordersTask, categoriesTask, productsTask, billsTask);

                ApplyDashboard(dashboardTask.Result);
                ApplyOpenBills(billsTask.Result);

                Orders.Clear();
                foreach (var o in ordersTask.Result.OrderByDescending(o => o.CreatedAt))
                    Orders.Add(new OrderCardViewModel(o));

                _allProducts = productsTask.Result.Select(p => new ManagerProductViewModel(p)).ToList();
                BuildCategoryFilters(categoriesTask.Result);
            });
        }

        // ---------------- Ações: visão geral ----------------

        [RelayCommand]
        private async Task ChangeTableAsync()
        {
            if (Page is { } page) await Table.PromptAsync(page);
        }

        [RelayCommand]
        private async Task OpenKitchenAsync() => await Shell.Current.GoToAsync(nameof(KitchenPage));

        [RelayCommand]
        private async Task LogoutAsync() => await Shell.Current.GoToAsync("//MenuPage"); // o cardápio encerra a sessão

        // ---------------- Ações: pedidos ----------------

        [RelayCommand]
        private async Task AdvanceOrderAsync(OrderCardViewModel? card)
        {
            if (card is null || card.IsFinished) return;
            await RunAsync(async () => card.Update(await _admin.SetOrderStatusAsync(card.Id, card.NextStatus)), showBusy: false);
        }

        [RelayCommand]
        private async Task CancelOrderAsync(OrderCardViewModel? card)
        {
            if (card is null || card.IsFinished || Page is not { } page) return;

            var confirm = await page.DisplayAlertAsync(
                $"Cancelar {card.Title} · #{card.Id}?",
                "O pedido sai da tela da cozinha e não entra no faturamento. Isso não pode ser desfeito.",
                "Cancelar pedido", "Voltar");
            if (!confirm) return;

            await RunAsync(async () =>
            {
                card.Update(await _admin.SetOrderStatusAsync(card.Id, OrderStatus.Cancelled));
                await ReloadDashboardAsync();
            }, showBusy: false);
        }

        // ---------------- Ações: usuários ----------------

        [RelayCommand]
        private async Task NewUserAsync() => await Shell.Current.GoToAsync($"{nameof(UserEditPage)}?id=0");

        [RelayCommand]
        private async Task EditUserAsync(ManagerUserViewModel? item)
        {
            if (item is not null)
                await Shell.Current.GoToAsync($"{nameof(UserEditPage)}?id={item.Id}");
        }

        // ---------------- Ações: mesas abertas ----------------

        [RelayCommand]
        private async Task CloseOpenBillAsync(OpenBillViewModel? bill)
        {
            if (bill is null || !bill.CanClose || Page is not { } page) return;

            var confirm = await page.DisplayAlertAsync(
                $"Encerrar a conta da {bill.Title}?",
                $"Total: {bill.TotalText}. A conta será marcada como paga (simulação) e o próximo pedido da mesa abrirá uma conta nova.",
                "Encerrar conta", "Voltar");
            if (!confirm) return;

            await RunAsync(async () =>
            {
                await _admin.CloseBillAsync(bill.Id);
                await ReloadDashboardAsync();
            }, showBusy: false);

            // Em sucesso a mesa sai da lista; se o servidor recusou (ex.: pedido em andamento), mostra o estado real.
            await ReloadOpenBillsAsync();
        }

        // ---------------- Ações: cardápio ----------------

        [RelayCommand]
        private void FilterCategory(CategoryFilterViewModel? filter)
        {
            if (filter is null) return;
            foreach (var f in CategoryFilters) f.IsSelected = ReferenceEquals(f, filter);
            _categoryFilter = filter.Category;
            ApplyProductFilter();
        }

        [RelayCommand]
        private async Task NewCategoryAsync()
        {
            if (Page is not { } page) return;
            var name = await page.DisplayPromptAsync("Nova categoria", "Nome da categoria:", "Criar", "Cancelar", maxLength: 80);
            if (string.IsNullOrWhiteSpace(name)) return;

            var order = CategoryFilters.Where(f => f.Category is not null).Select(f => f.Category!.DisplayOrder).DefaultIfEmpty(0).Max() + 1;
            await RunAsync(async () =>
            {
                await _admin.CreateCategoryAsync(new SaveCategoryRequest { Name = name.Trim(), DisplayOrder = order });
                BuildCategoryFilters(await _admin.GetCategoriesAsync());
            }, showBusy: false);
        }

        /// <summary>Toque longo/botão na categoria selecionada: renomear ou excluir.</summary>
        [RelayCommand]
        private async Task EditCategoryAsync()
        {
            if (_categoryFilter is not { } category || Page is not { } page) return;

            var action = await page.DisplayActionSheetAsync(category.Name, "Fechar", "Excluir", "Renomear");
            if (action == "Renomear")
            {
                var name = await page.DisplayPromptAsync("Renomear categoria", "Novo nome:", "Salvar", "Cancelar",
                    maxLength: 80, initialValue: category.Name);
                if (string.IsNullOrWhiteSpace(name)) return;

                await RunAsync(async () =>
                {
                    await _admin.UpdateCategoryAsync(category.Id, new SaveCategoryRequest { Name = name.Trim(), DisplayOrder = category.DisplayOrder });
                    await ReloadMenuAsync();
                }, showBusy: false);
            }
            else if (action == "Excluir")
            {
                if (!await page.DisplayAlertAsync($"Excluir \"{category.Name}\"?",
                        "Só é possível excluir categorias sem produtos.", "Excluir", "Voltar"))
                    return;

                await RunAsync(async () =>
                {
                    await _admin.DeleteCategoryAsync(category.Id);
                    _categoryFilter = null;
                    BuildCategoryFilters(await _admin.GetCategoriesAsync());
                }, showBusy: false);
            }
        }

        [RelayCommand]
        private async Task ToggleAvailabilityAsync(ManagerProductViewModel? item)
        {
            if (item is null || item.IsUpdating) return;
            item.IsUpdating = true;
            try
            {
                await RunAsync(async () => item.Update(await _admin.SetAvailabilityAsync(item.Id, !item.IsAvailable)), showBusy: false);
            }
            finally
            {
                item.IsUpdating = false;
            }
        }

        [RelayCommand]
        private async Task NewProductAsync()
        {
            var categoryId = _categoryFilter?.Id ?? 0;
            await Shell.Current.GoToAsync($"{nameof(ProductEditPage)}?id=0&categoryId={categoryId}");
        }

        [RelayCommand]
        private async Task EditProductAsync(ManagerProductViewModel? item)
        {
            if (item is not null)
                await Shell.Current.GoToAsync($"{nameof(ProductEditPage)}?id={item.Id}");
        }

        /// <summary>Chamado ao voltar do editor de produto.</summary>
        public async Task ReloadMenuAsync()
        {
            await RunAsync(async () =>
            {
                var categories = _admin.GetCategoriesAsync();
                var products = _admin.GetProductsAsync();
                await Task.WhenAll(categories, products);
                _allProducts = products.Result.Select(p => new ManagerProductViewModel(p)).ToList();
                BuildCategoryFilters(categories.Result);
            }, showBusy: false);
        }

        // ---------------- Tempo real ----------------

        private void OnOrderEvent(OrderDto dto) =>
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var (from, to) = Today();
                if (dto.CreatedAt < from || dto.CreatedAt >= to) return;

                var card = Orders.FirstOrDefault(o => o.Id == dto.Id);
                if (card is null) Orders.Insert(0, new OrderCardViewModel(dto));
                else card.Update(dto);

                await ReloadDashboardAsync();
                if (IsTables) await ReloadOpenBillsAsync();
            });

        private void OnReconnected() => MainThread.BeginInvokeOnMainThread(async () => await RefreshAsync());

        // ---------------- Auxiliares ----------------

        private static Page? Page => Shell.Current?.CurrentPage;

        /// <summary>Início e fim do dia de hoje no fuso do tablet, em UTC.</summary>
        private static (DateTime From, DateTime To) Today()
        {
            var start = DateTime.Today.ToUniversalTime();
            return (start, start.AddDays(1));
        }

        private async Task ReloadDashboardAsync()
        {
            if (_dashboardLoading) return;
            _dashboardLoading = true;
            try
            {
                var (from, to) = Today();
                ApplyDashboard(await _admin.GetDashboardAsync(from, to));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Falha ao atualizar indicadores: {ex.Message}");
            }
            finally
            {
                _dashboardLoading = false;
            }
        }

        /// <summary>Recarrega a lista de usuários (ao abrir a aba e ao voltar do cadastro/edição).</summary>
        public async Task ReloadUsersAsync()
        {
            try
            {
                var users = await _admin.GetUsersAsync();
                Users.Clear();
                foreach (var u in users)
                    Users.Add(new ManagerUserViewModel(u));

                OnPropertyChanged(nameof(HasUsers));
                OnPropertyChanged(nameof(HasNoUsers));
                OnPropertyChanged(nameof(UsersSummary));
            }
            catch (Exception ex) when (ex is StaffAccessException or OrderRejectedException)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                ErrorMessage = "Não foi possível carregar os usuários. Verifique se a API está em execução.";
            }
        }

        private async Task ReloadOpenBillsAsync()
        {
            if (_billsLoading) return;
            _billsLoading = true;
            try
            {
                ApplyOpenBills(await _admin.GetOpenBillsAsync());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Falha ao atualizar mesas abertas: {ex.Message}");
            }
            finally
            {
                _billsLoading = false;
            }
        }

        /// <summary>Reconcilia a lista com o servidor sem recriar os cards que já existem.</summary>
        private void ApplyOpenBills(List<OpenBill> bills)
        {
            var ids = bills.Select(b => b.Id).ToHashSet();
            for (var i = OpenBills.Count - 1; i >= 0; i--)
                if (!ids.Contains(OpenBills[i].Id))
                    OpenBills.RemoveAt(i);

            foreach (var dto in bills.OrderBy(b => b.TableNumber))
            {
                var existing = OpenBills.FirstOrDefault(b => b.Id == dto.Id);
                if (existing is not null)
                {
                    existing.Update(dto);
                    continue;
                }

                var index = 0;
                while (index < OpenBills.Count && OpenBills[index].TableNumber < dto.TableNumber)
                    index++;
                OpenBills.Insert(index, new OpenBillViewModel(dto));
            }

            OnPropertyChanged(nameof(HasOpenBills));
            OnPropertyChanged(nameof(HasNoOpenBills));
            OnPropertyChanged(nameof(OpenBillsSummary));
        }

        private void ApplyDashboard(Dashboard d)
        {
            TopProducts.Clear();
            var i = 1;
            foreach (var t in d.TopProducts)
                TopProducts.Add(new TopProductViewModel(i++, t.Name, $"{t.Quantity} un.", $"{t.Revenue:C2}"));
            Dashboard = d;
        }

        private void BuildCategoryFilters(List<CategoryAdmin> categories)
        {
            if (_categoryFilter is not null)
                _categoryFilter = categories.FirstOrDefault(c => c.Id == _categoryFilter.Id);

            CategoryFilters.Clear();
            CategoryFilters.Add(new CategoryFilterViewModel(null, _allProducts.Count) { IsSelected = _categoryFilter is null });
            foreach (var c in categories)
                CategoryFilters.Add(new CategoryFilterViewModel(c, c.ProductCount) { IsSelected = c.Id == _categoryFilter?.Id });

            OnPropertyChanged(nameof(HasCategorySelected));
            ApplyProductFilter();
        }

        public bool HasCategorySelected => _categoryFilter is not null;

        private void ApplyProductFilter()
        {
            Products.Clear();
            foreach (var p in _allProducts.Where(p => _categoryFilter is null || p.Product.CategoryId == _categoryFilter.Id))
                Products.Add(p);
            OnPropertyChanged(nameof(HasCategorySelected));
        }

        /// <summary>Executa uma chamada à API mostrando erros de forma amigável.</summary>
        private async Task RunAsync(Func<Task> action, bool showBusy = true)
        {
            if (showBusy) IsBusy = true;
            ErrorMessage = null;
            try
            {
                await action();
            }
            catch (StaffAccessException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (OrderRejectedException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                ErrorMessage = "Não foi possível falar com o servidor. Verifique se a API está em execução.";
            }
            finally
            {
                if (showBusy) IsBusy = false;
            }
        }

        private static string Minutes(double? value) => value switch
        {
            null => "—",
            < 1 => "< 1 min",
            { } v => $"{v:0.#} min"
        };

        private static string Ms(LatencyStats? l, Func<LatencyStats, double> pick) =>
            l is { Samples: > 0 } ? $"{pick(l):0} ms" : "—";
    }
}
