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

        public MenuViewModel(MenuApiService api, CartService cart)
        {
            _api = api;
            Cart = cart;
        }

        public CartService Cart { get; }
        public ObservableCollection<Category> Categories { get; } = new();
        public ObservableCollection<ProductItemViewModel> Products { get; } = new();

        [ObservableProperty]
        private Category? selectedCategory;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? errorMessage;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        partial void OnSelectedCategoryChanged(Category? value)
        {
            Products.Clear();
            if (value is null) return;

            foreach (var product in value.Products)
                Products.Add(new ProductItemViewModel(product));
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

                Categories.Clear();
                foreach (var category in menu)
                    Categories.Add(category);

                SelectedCategory = Categories.FirstOrDefault();
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
            if (item is not null)
                Cart.Add(item.Product);
        }

        [RelayCommand]
        private async Task OpenCartAsync() =>
            await Shell.Current.GoToAsync(nameof(CartPage));
    }
}
