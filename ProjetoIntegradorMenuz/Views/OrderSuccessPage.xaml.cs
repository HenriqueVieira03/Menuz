namespace ProjetoIntegradorMenuz.Views;

public partial class OrderSuccessPage : ContentPage
{
    public OrderSuccessPage()
    {
        InitializeComponent();
    }

    private async void VoltarMenu_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();
    }
}