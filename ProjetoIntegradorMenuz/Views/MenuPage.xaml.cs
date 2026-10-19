using System.ComponentModel;
using ProjetoIntegradorMenuz.Services;
using ProjetoIntegradorMenuz.ViewModels;

namespace ProjetoIntegradorMenuz.Views
{
    public partial class MenuPage : ContentPage
    {
        private readonly MenuViewModel _vm;
        private bool _barVisible;

        public MenuPage(MenuViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;

            SizeChanged += OnPageSizeChanged;
            _vm.PropertyChanged += OnViewModelPropertyChanged;
            _vm.Cart.PropertyChanged += OnCartPropertyChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_vm.Chips.Count == 0)
                await _vm.LoadCommand.ExecuteAsync(null);
        }

        // Colunas conforme a largura (tablet = 3+, celular = 1-2).
        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            if (Width <= 0) return;
            ProductsLayout.Span = Math.Max(1, (int)((Width - 40) / 300));
        }

        // Lazy loading: a imagem só é solicitada quando o card é realizado/reciclado na tela.
        private void OnCardBindingContextChanged(object? sender, EventArgs e)
        {
            if (sender is BindableObject { BindingContext: ProductItemViewModel item })
                item.LoadImage();
        }

        // Transição suave ao trocar de categoria.
        private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(MenuViewModel.Products)) return;

            ProductsList.Opacity = 0;
            ProductsList.TranslationY = 16;
            await Task.WhenAll(
                ProductsList.FadeTo(1, 220, Easing.CubicOut),
                ProductsList.TranslateTo(0, 0, 220, Easing.CubicOut));
        }

        // Barra do carrinho desliza para dentro/fora e dá um leve "pulso" a cada item.
        private async void OnCartPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(CartService.TotalCount)) return;

            var show = _vm.Cart.TotalCount > 0;

            if (show == _barVisible)
            {
                if (show)
                {
                    await CartBar.ScaleTo(1.03, 80, Easing.CubicOut);
                    await CartBar.ScaleTo(1, 120, Easing.CubicIn);
                }
                return;
            }

            _barVisible = show;

            if (show)
            {
                CartBar.TranslationY = 120;
                CartBar.IsVisible = true;
                await CartBar.TranslateTo(0, 0, 280, Easing.CubicOut);
            }
            else
            {
                await CartBar.TranslateTo(0, 120, 200, Easing.CubicIn);
                if (!_barVisible)
                    CartBar.IsVisible = false;
            }
        }
    }
}
