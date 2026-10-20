using ProjetoIntegradorMenuz.ViewModels;

namespace ProjetoIntegradorMenuz.Views
{
    public partial class KitchenPage : ContentPage
    {
        private readonly KitchenViewModel _vm;

        public KitchenPage(KitchenViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
            SizeChanged += OnPageSizeChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.SyncCommand.ExecuteAsync(null); // sincroniza ao abrir; depois, pelo botão
        }

        // Colunas conforme a largura (tablet = 3+, celular = 1-2), igual ao cardápio.
        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            if (Width <= 0) return;
            OrdersLayout.Span = Math.Max(1, (int)((Width - 40) / 320));
        }
    }
}