using ProjetoIntegradorMenuz.ViewModels;

namespace ProjetoIntegradorMenuz.Views
{
    public partial class MyOrdersPage : ContentPage
    {
        private readonly MyOrdersViewModel _vm;

        public MyOrdersPage(MyOrdersViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
            SizeChanged += OnPageSizeChanged;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.StartAsync(); // carrega a conta da mesa e passa a atualizar sozinho
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _vm.Stop();
        }

        // Colunas conforme a largura (tablet = 2+, celular = 1), no mesmo estilo das outras telas.
        private void OnPageSizeChanged(object? sender, EventArgs e)
        {
            if (Width <= 0) return;
            OrdersLayout.Span = Math.Max(1, (int)((Width - 40) / 340));
        }
    }
}
