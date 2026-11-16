using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoIntegradorMenuz.Models;
using ProjetoIntegradorMenuz.Services;
using ProjetoIntegradorMenuz.Views;

namespace ProjetoIntegradorMenuz.ViewModels
{
    public partial class MenuViewModel : ObservableObject
    {
        private readonly MenuApiService _api;

        // Cache dos itens por categoria: trocar de aba não recria views nem baixa imagens de novo.
        private readonly Dictionary<int, IReadOnlyList<ProductItemViewModel>> _cache = new();

        public MenuViewModel(MenuApiService api, CartService cart, TableService table)
        {
            _api = api;
            Cart = cart;
            Table = table;
        }

        public CartService Cart { get; }
        public TableService Table { get; }

        /// <summary>Produto aberto no popup de personalização (null = popup fechado).</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCustomizing))]
        private ProductCustomizationViewModel? customization;

        public bool IsCustomizing => Customization is not null;
        public ObservableCollection<CategoryChipViewModel> Chips { get; } = new();

        // A lista é trocada de uma vez só (uma notificação), em vez de Clear + Add item a item.
        [ObservableProperty]
        private IReadOnlyList<ProductItemViewModel> products = Array.Empty<ProductItemViewModel>();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SectionTitle))]
        private Category? selectedCategory;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? errorMessage;

        // Quando a categoria "Todos" estiver selecionada, o título ficará "Todos"
        public string SectionTitle => SelectedCategory?.Name ?? string.Empty;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        partial void OnSelectedCategoryChanged(Category? value)
        {
            if (value is null)
            {
                Products = Array.Empty<ProductItemViewModel>();
                return;
            }

            if (!_cache.TryGetValue(value.Id, out var list))
            {
                list = value.Products.Select(p => new ProductItemViewModel(p)).ToList();
                _cache[value.Id] = list;
            }

            Products = list;
        }

        [RelayCommand]
        private void SelectCategory(CategoryChipViewModel? chip)
        {
            if (chip is null || chip.IsSelected) return;

            foreach (var c in Chips)
                c.IsSelected = ReferenceEquals(c, chip);

            SelectedCategory = chip.Category;
        }
        [RelayCommand]
        private async Task OpenStaffAsync() =>
            await Shell.Current.GoToAsync(nameof(LoginPage));

        [RelayCommand]
        private async Task LoadAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var menu = await _api.GetMenuAsync();

                _cache.Clear();
                Chips.Clear();

                // 1. Pega todos os produtos de todas as categorias recebidas
                var todosProdutos = menu.SelectMany(c => c.Products).ToList();

                // 2. Cria uma categoria fictícia "Todos"
                var categoriaTodos = new Category
                {
                    Id = 0,
                    Name = "Todos",
                    Products = todosProdutos
                    
                };

                // 3. Adiciona "Todos" como o primeiro botão da lista
                Chips.Add(new CategoryChipViewModel(categoriaTodos));

                // 4. Adiciona o resto das categorias originais
                foreach (var category in menu)
                    Chips.Add(new CategoryChipViewModel(category));

                // Seleciona a categoria "Todos" por padrão ao abrir o app
                SelectCategory(Chips.FirstOrDefault());
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                ErrorMessage = "Não foi possível carregar o cardápio. Verifique se a API está em execução.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Botão "Adicionar" do card: se o produto tem escolhas, abre o popup;
        /// senão adiciona direto (com o tamanho único, se houver).
        /// </summary>
        [RelayCommand]
        private void AddToCart(ProductItemViewModel? item)
        {
            if (item is null) return;

            if (item.NeedsChoices)
            {
                OpenProduct(item);
                return;
            }

            Cart.Add(new CartItem(item.Product, size: item.Product.Sizes.FirstOrDefault()));
            _ = item.FlashAddedAsync();
        }

        /// <summary>Toque no card: abre os detalhes/personalização do produto.</summary>
        [RelayCommand]
        private void OpenProduct(ProductItemViewModel? item)
        {
            if (item is null) return;
            item.LoadImage();
            Customization = new ProductCustomizationViewModel(item);
        }

        [RelayCommand]
        private void CloseProduct() => Customization = null;

        [RelayCommand]
        private void ConfirmProduct()
        {
            if (Customization is null) return;

            Cart.Add(Customization.BuildCartItem());
            _ = Customization.Item.FlashAddedAsync();
            Customization = null;
        }

        [RelayCommand]
        private async Task SetTableAsync()
        {
            if (Shell.Current?.CurrentPage is not { } page) return;

            // Primeira configuração é livre; trocar a mesa depois fica com a gerência (evita cliente mudar por engano).
            if (!Table.IsConfigured)
            {
                await Table.PromptAsync(page);
                return;
            }

            var goToLogin = await page.DisplayAlertAsync(
                Table.DisplayText,
                "Para trocar a mesa deste tablet, entre como gerente.",
                "Entrar", "Fechar");
            if (goToLogin)
                await Shell.Current.GoToAsync(nameof(LoginPage));
        }

        /// <summary>Recarrega o cardápio (ex.: depois que a gerência alterou produtos).</summary>
        public async Task ReloadAsync()
        {
            Customization = null;
            Chips.Clear();
            await LoadCommand.ExecuteAsync(null);
        }

        /// <summary>Abre "Meus pedidos". Sem mesa definida, pergunta a mesa antes (sem ela não há conta para mostrar).</summary>
        [RelayCommand]
        private async Task OpenMyOrdersAsync()
        {
            if (!Table.IsConfigured)
            {
                if (Shell.Current?.CurrentPage is not { } page || !await Table.PromptAsync(page))
                    return;
            }

            await Shell.Current.GoToAsync(nameof(MyOrdersPage));
        }

        [RelayCommand]
        private async Task OpenCartAsync() =>
            await Shell.Current.GoToAsync(nameof(CartPage));
    }
}