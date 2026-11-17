using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoIntegradorMenuz.Models;
using ProjetoIntegradorMenuz.Services;
using ProjetoIntegradorMenuz.Views;

namespace ProjetoIntegradorMenuz.ViewModels
{
    public enum ManagerTab { Overview, Orders, Tables, Users, Menu, Appearance }

    /// <summary>Item da barra lateral de navegação do painel.</summary>
    public partial class NavItemViewModel(ManagerTab tab, string label, string icon) : ObservableObject
    {
        public ManagerTab Tab { get; } = tab;
        public string Key => Tab.ToString();
        public string Label { get; } = label;
        public string Icon { get; } = icon;

        [ObservableProperty]
        private bool isSelected;
    }

    /// <summary>Opção de cor de destaque na tela Aparência.</summary>
    public partial class AccentOptionViewModel(AccentPreset preset) : ObservableObject
    {
        public AccentPreset Preset { get; } = preset;
        public string Name => Preset.Name;
        public Color Swatch => Preset.Primary;

        [ObservableProperty]
        private bool isSelected;
    }

    /// <summary>Opção de tema (claro/escuro) na tela Aparência.</summary>
    public partial class ThemeModeOptionViewModel(AppThemeMode mode, string label, string description) : ObservableObject
    {
        public AppThemeMode Mode { get; } = mode;
        public string Label { get; } = label;
        public string Description { get; } = description;
        public bool IsDark => Mode == AppThemeMode.Dark;

        [ObservableProperty]
        private bool isSelected;
    }

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

        public ManagerViewModel(AdminApiService admin, AuthService auth, OrderHubClient hub, TableService table, ThemeService theme)
        {
            _admin = admin;
            _auth = auth;
            _hub = hub;
            Table = table;
            Theme = theme;

            NavItems = new[]
            {
                new NavItemViewModel(ManagerTab.Overview, "Visão geral", "grid") { IsSelected = true },
                new NavItemViewModel(ManagerTab.Orders, "Pedidos", "list"),
                new NavItemViewModel(ManagerTab.Tables, "Mesas", "hash"),
                new NavItemViewModel(ManagerTab.Menu, "Cardápio", "book"),
                new NavItemViewModel(ManagerTab.Users, "Usuários", "users"),
                new NavItemViewModel(ManagerTab.Appearance, "Aparência", "droplet"),
            };

            AccentOptions = ThemeService.Presets.Select(p => new AccentOptionViewModel(p)).ToList();
            ModeOptions = new[]
            {
                new ThemeModeOptionViewModel(AppThemeMode.Light, "Claro", "Fundo creme, ideal para o salão"),
                new ThemeModeOptionViewModel(AppThemeMode.Dark, "Escuro", "Menos brilho, bom para a cozinha e ambientes escuros"),
            };
        }

        public TableService Table { get; }
        public ThemeService Theme { get; }
        public string UserText => _auth.DisplayName;
        public string UserInitial => string.IsNullOrEmpty(_auth.DisplayName) ? "?" : _auth.DisplayName[..1].ToUpperInvariant();

        // ---------------- Navegação ----------------

        public IReadOnlyList<NavItemViewModel> NavItems { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOverview))]
        [NotifyPropertyChangedFor(nameof(IsOrders))]
        [NotifyPropertyChangedFor(nameof(IsTables))]
        [NotifyPropertyChangedFor(nameof(IsUsers))]
        [NotifyPropertyChangedFor(nameof(IsMenu))]
        [NotifyPropertyChangedFor(nameof(IsAppearance))]
        [NotifyPropertyChangedFor(nameof(IsNotAppearance))]
        [NotifyPropertyChangedFor(nameof(ShowEditCategory))]
        [NotifyPropertyChangedFor(nameof(PageTitle))]
        [NotifyPropertyChangedFor(nameof(PageSubtitle))]
        private ManagerTab selectedTab = ManagerTab.Overview;

        public bool IsOverview => SelectedTab == ManagerTab.Overview;
        public bool IsOrders => SelectedTab == ManagerTab.Orders;
        public bool IsTables => SelectedTab == ManagerTab.Tables;
        public bool IsUsers => SelectedTab == ManagerTab.Users;
        public bool IsMenu => SelectedTab == ManagerTab.Menu;
        public bool IsAppearance => SelectedTab == ManagerTab.Appearance;
        public bool IsNotAppearance => !IsAppearance;
        public bool ShowEditCategory => IsMenu && HasCategorySelected;

        public string PageTitle => NavItems.First(n => n.Tab == SelectedTab).Label;

        public string PageSubtitle => SelectedTab switch
        {
            ManagerTab.Overview => $"Indicadores de hoje, {DateTime.Now:dd 'de' MMMM}",
            ManagerTab.Orders => Orders.Count == 1 ? "1 pedido hoje" : $"{Orders.Count} pedidos hoje",
            ManagerTab.Tables => OpenBillsSummary,
            ManagerTab.Menu => $"{_allProducts.Count} produtos em {Math.Max(0, CategoryFilters.Count - 1)} categorias",
            ManagerTab.Users => UsersSummary,
            _ => "Nome, cor de destaque e tema do aplicativo em todos os aparelhos"
        };

        // Ao abrir a aba de mesas, já mostra as contas como estão agora.
        partial void OnSelectedTabChanged(ManagerTab oldValue, ManagerTab newValue)
        {
            foreach (var item in NavItems) item.IsSelected = item.Tab == newValue;

            if (newValue == ManagerTab.Tables) _ = ReloadOpenBillsAsync();
            if (newValue == ManagerTab.Users) _ = ReloadUsersAsync();
            if (newValue == ManagerTab.Appearance) LoadAppearanceForm();

            // Saiu da Aparência sem salvar: desfaz a pré-visualização.
            if (oldValue == ManagerTab.Appearance && newValue != ManagerTab.Appearance) Theme.ApplyCached();
        }

        // ---------------- Aparência ----------------

        public IReadOnlyList<AccentOptionViewModel> AccentOptions { get; }
        public IReadOnlyList<ThemeModeOptionViewModel> ModeOptions { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NamePreviewInitial))]
        private string restaurantNameInput = string.Empty;

        public string NamePreviewInitial => string.IsNullOrWhiteSpace(RestaurantNameInput) ? "?" : RestaurantNameInput.Trim()[..1].ToUpperInvariant();

        [ObservableProperty]
        private string? appearanceMessage;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveAppearanceCommand))]
        private bool isSavingAppearance;

        private string SelectedAccentKey => AccentOptions.FirstOrDefault(a => a.IsSelected)?.Preset.Key ?? ThemeService.Presets[0].Key;
        private AppThemeMode SelectedMode => ModeOptions.FirstOrDefault(m => m.IsSelected)?.Mode ?? AppThemeMode.Light;

        private void LoadAppearanceForm()
        {
            RestaurantNameInput = Theme.RestaurantName;
            foreach (var a in AccentOptions) a.IsSelected = a.Preset.Key == Theme.Accent.Key;
            foreach (var m in ModeOptions) m.IsSelected = m.Mode == Theme.Mode;
            AppearanceMessage = null;
        }

        [RelayCommand]
        private void SelectAccent(AccentOptionViewModel? option)
        {
            if (option is null) return;
            foreach (var a in AccentOptions) a.IsSelected = ReferenceEquals(a, option);
            Theme.Preview(SelectedAccentKey, SelectedMode); // mostra na hora, antes de salvar
            AppearanceMessage = null;
        }

        [RelayCommand]
        private void SelectMode(ThemeModeOptionViewModel? option)
        {
            if (option is null) return;
            foreach (var m in ModeOptions) m.IsSelected = ReferenceEquals(m, option);
            Theme.Preview(SelectedAccentKey, SelectedMode);
            AppearanceMessage = null;
        }

        [RelayCommand]
        private void ResetAppearance()
        {
            RestaurantNameInput = ThemeService.DefaultName;
            SelectAccent(AccentOptions[0]);
            SelectMode(ModeOptions[0]);
        }

        private bool CanSaveAppearance() => !IsSavingAppearance;

        [RelayCommand(CanExecute = nameof(CanSaveAppearance))]
        private async Task SaveAppearanceAsync()
        {
            var name = RestaurantNameInput?.Trim() ?? string.Empty;
            if (name.Length is 0 or > 40)
            {
                ErrorMessage = "Informe o nome do estabelecimento (até 40 caracteres).";
                return;
            }

            IsSavingAppearance = true;
            try
            {
                await RunAsync(async () =>
                {
                    var saved = await _admin.SaveAppearanceAsync(new Appearance
                    {
                        RestaurantName = name,
                        AccentKey = SelectedAccentKey,
                        ThemeMode = SelectedMode == AppThemeMode.Dark ? "Dark" : "Light"
                    });
                    Theme.Apply(saved);
                    AppearanceMessage = "Aparência salva. Os outros aparelhos aplicam ao abrir o cardápio.";
                }, showBusy: false);
            }
            finally
            {
                IsSavingAppearance = false;
            }
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
            OnPropertyChanged(nameof(UserInitial));

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
            if (IsAppearance) Theme.ApplyCached(); // painel fechado com a pré-visualização aberta

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
                OnPropertyChanged(nameof(PageSubtitle));
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
                OnPropertyChanged(nameof(PageSubtitle));

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
                OnPropertyChanged(nameof(PageSubtitle));
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
            OnPropertyChanged(nameof(PageSubtitle));
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
            OnPropertyChanged(nameof(ShowEditCategory));
            ApplyProductFilter();
        }

        public bool HasCategorySelected => _categoryFilter is not null;

        private void ApplyProductFilter()
        {
            Products.Clear();
            foreach (var p in _allProducts.Where(p => _categoryFilter is null || p.Product.CategoryId == _categoryFilter.Id))
                Products.Add(p);
            OnPropertyChanged(nameof(HasCategorySelected));
            OnPropertyChanged(nameof(ShowEditCategory));
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
