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

            // Verifica se o carrinho está vazio antes de prosseguir
            if (vm.Cart.TotalCount == 0)
            {
                await DisplayAlert("Carrinho Vazio", "Adicione pelo menos um item antes de finalizar o pedido.", "OK");
                return; 
            }

            // Limpa o carrinho
            vm.ClearCommand.Execute(null);

            // Navega para a tela de sucesso
            await Navigation.PushAsync(new OrderSuccessPage());
        }
    }
}