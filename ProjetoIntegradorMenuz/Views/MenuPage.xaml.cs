using System.ComponentModel;
using ProjetoIntegradorMenuz.Services;
using ProjetoIntegradorMenuz.ViewModels;

namespace ProjetoIntegradorMenuz.Views
{
    public partial class MenuPage : ContentPage
    {
        private readonly MenuViewModel _vm;
        private bool _cartBarShown;

        public MenuPage(MenuViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
            SizeChanged += OnPageSizeChanged;
            _vm.Cart.PropertyChanged += OnCartPropertyChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            SyncCartBar();

            if (_vm.Categories.Count == 0)
                await _vm.LoadCommand.ExecuteAsync(null);
        }

        // Ajusta o número de colunas conforme a largura (tablet = 3+, celular = 1-2).
        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            if (Width <= 0) return;
            ProductsLayout.Span = Math.Max(1, (int)((Width - 40) / 320));
        }

        // Lazy loading: a imagem só é solicitada quando o card é realizado/reciclado na tela.
        private void OnCardBindingContextChanged(object? sender, EventArgs e)
        {
            if (sender is BindableObject { BindingContext: ProductItemViewModel item })
                item.LoadImage();
        }

        // ---------- Barra flutuante do carrinho ----------

        // Ao voltar da tela do carrinho (ex.: "Limpar"), reajusta sem animação.
        private void SyncCartBar()
        {
            _cartBarShown = _vm.Cart.TotalCount > 0;
            CartBar.IsVisible = _cartBarShown;
            CartBar.TranslationY = 0;
            CartBar.Scale = 1;
        }

        private async void OnCartPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(CartService.TotalCount)) return;

            var count = _vm.Cart.TotalCount;

            if (count > 0 && !_cartBarShown)
            {
                // Primeiro item: a barra desliza de baixo para cima.
                _cartBarShown = true;
                CartBar.TranslationY = 140;
                CartBar.IsVisible = true;
                await CartBar.TranslateToAsync(0, 0, 280, Easing.CubicOut);
            }
            else if (count > 0)
            {
                // Itens seguintes: um "pulinho" confirma que o toque funcionou.
                await CartBar.ScaleToAsync(1.04, 80, Easing.CubicOut);
                await CartBar.ScaleToAsync(1.0, 120, Easing.CubicIn);
            }
            else if (_cartBarShown)
            {
                _cartBarShown = false;
                await CartBar.TranslateToAsync(0, 140, 200, Easing.CubicIn);
                CartBar.IsVisible = false;
            }
        }
    }
}
