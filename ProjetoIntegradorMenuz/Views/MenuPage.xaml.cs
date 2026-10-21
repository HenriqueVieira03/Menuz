using System.ComponentModel;
using ProjetoIntegradorMenuz.Services;
using ProjetoIntegradorMenuz.ViewModels;

namespace ProjetoIntegradorMenuz.Views
{
    public partial class MenuPage : ContentPage
    {
        private readonly MenuViewModel _vm;
        private bool _barVisible;

        private int _quantidadePopup = 1;
        private ProductItemViewModel _produtoSelecionado;

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

        // Método para abrir o popup ao clicar em qualquer lugar do card do produto
        private void CardProduto_Tapped(object sender, TappedEventArgs e)
        {
            var card = (Border)sender;
            var produto = (ProjetoIntegradorMenuz.ViewModels.ProductItemViewModel)card.BindingContext;

            if (produto != null)
            {
                // Salva o produto na memória e reseta a quantidade para 1
                _produtoSelecionado = produto;
                _quantidadePopup = 1;

                PopupNome.Text = produto.Name;
                PopupDescricao.Text = produto.Description;
                PopupImagem.Source = produto.Picture;

                AtualizarValoresPopup();
                PopupProduto.IsVisible = true;
            }
        }

        private void FecharPopup_Clicked(object sender, EventArgs e)
        {
            PopupProduto.IsVisible = false;
        }

        private void PopupMenos_Clicked(object sender, EventArgs e)
        {
            if (_quantidadePopup > 1)
            {
                _quantidadePopup--;
                AtualizarValoresPopup();
            }
        }

        private void PopupMais_Clicked(object sender, EventArgs e)
        {
            _quantidadePopup++;
            AtualizarValoresPopup();
        }

        private void AtualizarValoresPopup()
        {
            PopupQuantidadeText.Text = _quantidadePopup.ToString();

            // Calcula o total (Preço do produto * Quantidade)
            var valorTotal = _produtoSelecionado.Price * _quantidadePopup;

            // Atualiza o texto do botão laranja igual ao do Fournos
            PopupBtnAdicionar.Text = $"Adicionar • {valorTotal:C2}";
        }

        private void PopupAdicionar_Clicked(object sender, EventArgs e)
        {
            if (_produtoSelecionado != null)
            {
                // Dispara o comando de adicionar ao carrinho N vezes com base na quantidade escolhida
                for (int i = 0; i < _quantidadePopup; i++)
                {
                    _vm.AddToCartCommand.Execute(_produtoSelecionado);
                }

                // Fecha o popup após adicionar
                PopupProduto.IsVisible = false;
            }
        }

        

        // Colunas conforme a largura (tablet = 3+, celular = 1-2).
        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            if (Width <= 0) return;
            ProductsLayout.Span = Math.Max(1, (int)((Width - 40) / 300));
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