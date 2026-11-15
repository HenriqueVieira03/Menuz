using ProjetoIntegradorMenuz.ViewModels;

namespace ProjetoIntegradorMenuz.Views
{
    public partial class CartPage : ContentPage
    {
        public CartPage(CartViewModel vm)
        {
            BindingContext = vm;
            InitializeComponent();
            SizeChanged += OnPageSizeChanged;
        }

        // Tablet/desktop: resumo ao lado da lista. Celular: resumo embaixo.
        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            if (Width <= 0) return;
            var wide = Width >= 820;

            ContentGrid.ColumnDefinitions = wide
                ? new ColumnDefinitionCollection(new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(360)))
                : new ColumnDefinitionCollection(new ColumnDefinition(GridLength.Star));
            ContentGrid.RowDefinitions = wide
                ? new RowDefinitionCollection(new RowDefinition(GridLength.Star))
                : new RowDefinitionCollection(new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto));

            Grid.SetColumn(SummaryCard, wide ? 1 : 0);
            Grid.SetRow(SummaryCard, wide ? 0 : 1);
        }

        private async void FinalizarPedido_Clicked(object sender, EventArgs e)
        {
            var vm = (CartViewModel)BindingContext;

            if (vm.Cart.TotalCount == 0)
            {
                await DisplayAlert("Carrinho Vazio", "Adicione pelo menos um item antes de finalizar o pedido.", "OK");
                return;
            }

            // Sem mesa definida a cozinha não sabe para onde levar o pedido: pergunta antes de enviar.
            if (!vm.Table.IsConfigured && !await vm.Table.PromptAsync(this))
                return;

            // Envia para a API (grava o pedido e notifica a cozinha em tempo real)
            var error = await vm.SubmitOrderAsync();
            if (error is not null)
            {
                await DisplayAlert("Pedido não enviado", error, "OK");
                return;
            }

            await Navigation.PushAsync(new OrderSuccessPage());
        }
    }
}