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

            // Envia para a API (grava na tabela de pedidos que a cozinha consulta)
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