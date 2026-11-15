using ProjetoIntegradorMenuz.ViewModels;

namespace ProjetoIntegradorMenuz.Views
{
    public partial class ProductEditPage : ContentPage
    {
        private readonly ProductEditViewModel _vm;
        private bool _loaded;

        public ProductEditPage(ProductEditViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_loaded) return;
            _loaded = true;
            await _vm.LoadAsync(); // os parâmetros ?id= e ?categoryId= já foram aplicados pelo Shell
        }
    }
}
