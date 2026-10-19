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

        public MenuViewModel(MenuApiService api, CartService cart)
        {
            _api = api;
            Cart = cart;
        }

        public CartService Cart { get; }
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
                foreach (var category in menu)
                    Chips.Add(new CategoryChipViewModel(category));

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

        [RelayCommand]
        private void AddToCart(ProductItemViewModel? item)
        {
            if (item is null) return;

            Cart.Add(item.Product);
            _ = item.FlashAddedAsync();
        }

        [RelayCommand]
        private async Task OpenCartAsync() =>
            await Shell.Current.GoToAsync(nameof(CartPage));
    }
}
