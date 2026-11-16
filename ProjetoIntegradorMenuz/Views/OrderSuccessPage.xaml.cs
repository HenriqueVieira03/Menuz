namespace ProjetoIntegradorMenuz.Views;

public partial class OrderSuccessPage : ContentPage
{
    public OrderSuccessPage()
    {
        InitializeComponent();
    }

    private async void Acompanhar_Clicked(object sender, EventArgs e)
    {
        // Volta ao cardápio e abre "Meus pedidos" por cima, para o botão voltar levar ao cardápio.
        await Navigation.PopToRootAsync();
        await Shell.Current.GoToAsync(nameof(MyOrdersPage));
    }

    private async void VoltarMenu_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();
    }
}