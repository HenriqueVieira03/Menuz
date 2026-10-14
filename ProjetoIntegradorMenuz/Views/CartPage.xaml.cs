using ProjetoIntegradorMenuz.ViewModels;

namespace ProjetoIntegradorMenuz.Views
{
    public partial class CartPage : ContentPage
    {
        public CartPage(CartViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        private async void OnBackClicked(object? sender, EventArgs e) =>
            await Shell.Current.GoToAsync("..");

        // Placeholder: o envio do pedido (POST + SignalR para o KDS) entra na próxima sprint.
        private async void OnCheckoutClicked(object? sender, EventArgs e) =>
            await DisplayAlert("Em breve", "O envio do pedido para a cozinha será ligado na próxima sprint.", "OK");
    }
}
