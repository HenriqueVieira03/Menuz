using ProjetoIntegradorMenuz.ViewModels;

namespace ProjetoIntegradorMenuz.Views
{
    public partial class CartPage : ContentPage
    {
        public CartPage(CartViewModel vm)
        {
            BindingContext = vm;
            InitializeComponent();
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