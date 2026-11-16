using ProjetoIntegradorMenuz.ViewModels;

namespace ProjetoIntegradorMenuz.Views
{
    public partial class CloseBillPage : ContentPage
    {
        private readonly CloseBillViewModel _vm;

        public CloseBillPage(CloseBillViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
            SizeChanged += OnPageSizeChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.StartAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _vm.Stop();
        }

        // Em tablet/desktop o resumo fica numa coluna central, como um recibo, em vez de esticar na tela toda.
        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            if (Width <= 0) return;
            var w = Math.Max(0, Math.Min(Width - 40, 720));
            ReceiptHost.WidthRequest = w;
            PayHost.WidthRequest = w;
        }

        private async void Pagar_Clicked(object sender, EventArgs e)
        {
            var (bill, error) = await _vm.PayAsync();

            if (error is not null)
            {
                await DisplayAlertAsync("Pagamento não realizado", error, "OK");
                await _vm.RefreshAsync(); // mostra a conta como está agora no servidor
                return;
            }

            _vm.Stop();
            await Navigation.PushAsync(new PaymentDonePage(bill!.TotalPaid ?? bill.Total, bill.TableNumber));
        }
    }
}
