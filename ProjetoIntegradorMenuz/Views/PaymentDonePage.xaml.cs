namespace ProjetoIntegradorMenuz.Views;

public partial class PaymentDonePage : ContentPage
{
    public PaymentDonePage(decimal totalPaid, int tableNumber)
    {
        InitializeComponent();
        AmountLabel.Text = totalPaid.ToString("C2");
        TableLabel.Text = tableNumber.ToString();
    }

    private async void VoltarMenu_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();
    }
}
