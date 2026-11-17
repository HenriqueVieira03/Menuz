using System.ComponentModel;
using ProjetoIntegradorMenuz.Services;
using ProjetoIntegradorMenuz.ViewModels;

namespace ProjetoIntegradorMenuz.Views
{
    public partial class MenuPage : ContentPage
    {
        private readonly MenuViewModel _vm;
        private readonly AuthService _auth;
        private bool _barVisible;

        public MenuPage(MenuViewModel vm, AuthService auth)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
            _auth = auth;

            SizeChanged += OnPageSizeChanged;
            _vm.PropertyChanged += OnViewModelPropertyChanged;
            _vm.Cart.PropertyChanged += OnCartPropertyChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Voltar ao cardápio do cliente encerra a sessão da equipe.
            // O cardápio é recarregado porque a gerência pode ter mudado produtos ou disponibilidade.
            if (_auth.Session is not null)
            {
                _auth.Logout();
                await _vm.ReloadAsync();
                return;
            }

            if (_vm.Chips.Count == 0)
                await _vm.LoadCommand.ExecuteAsync(null);
        }

        // Larguras a partir das quais aparecem a lista lateral de categorias e o painel do pedido.
        private const double RailMinWidth = 760;
        private const double PanelMinWidth = 1180;
        private bool _wide;

        /// <summary>
        /// Layout conforme a largura: tablet na horizontal mostra categorias + produtos + pedido;
        /// telas menores trocam o painel pela barra inferior e, no celular, a lista lateral por abas.
        /// </summary>
        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            if (Width <= 0) return;

            var showRail = Width >= RailMinWidth;
            _wide = Width >= PanelMinWidth;

            CategoryRail.IsVisible = showRail;
            CategoryTabs.IsVisible = !showRail;
            OrderPanel.IsVisible = _wide;
            HeaderCart.IsVisible = !_wide;
            ListFooter.HeightRequest = _wide ? 24 : 104;
            UpdateCartBar(animate: false);

            var productsWidth = Width - (showRail ? 232 : 0) - (_wide ? 340 : 0) - 48;
            ProductsLayout.Span = Math.Max(1, (int)(productsWidth / 240));

            // O detalhe do produto nunca passa da altura da tela: o conteúdo rola e o rodapé fica fixo.
            if (Height > 0)
                PopupCard.MaximumHeightRequest = Math.Max(320, Height - 40);
        }

        // Lazy loading: a imagem só é solicitada quando o card é desenhado/reciclado na tela.
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
            await ProductsList.FadeToAsync(1, 160, Easing.CubicOut);
        }

        private void OnCartPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CartService.TotalCount))
                UpdateCartBar(animate: true);
        }

        /// <summary>A barra inferior do pedido só existe quando o painel lateral não está na tela.</summary>
        private async void UpdateCartBar(bool animate)
        {
            var show = !_wide && _vm.Cart.TotalCount > 0;
            if (show == _barVisible) return;
            _barVisible = show;

            if (!animate)
            {
                CartBar.TranslationY = 0;
                CartBar.IsVisible = show;
                return;
            }

            if (show)
            {
                CartBar.TranslationY = 96;
                CartBar.IsVisible = true;
                await CartBar.TranslateToAsync(0, 0, 240, Easing.CubicOut);
            }
            else
            {
                await CartBar.TranslateToAsync(0, 96, 180, Easing.CubicIn);
                if (!_barVisible) CartBar.IsVisible = false;
            }
        }
    }
}