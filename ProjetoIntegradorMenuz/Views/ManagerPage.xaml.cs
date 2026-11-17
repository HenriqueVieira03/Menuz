using ProjetoIntegradorMenuz.ViewModels;

namespace ProjetoIntegradorMenuz.Views
{
    public partial class ManagerPage : ContentPage
    {
        private readonly ManagerViewModel _vm;
        private bool _loaded;

        public ManagerPage(ManagerViewModel vm)
        {
            InitializeComponent();
            BindingContext = _vm = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!_loaded)
            {
                _loaded = true;
                await _vm.StartAsync();
            }
            else
            {
                // Voltando do editor de produto ou da cozinha: só o cardápio pode ter mudado.
                await _vm.ReloadMenuAsync();
                if (_vm.IsUsers) await _vm.ReloadUsersAsync();
            }
        }

        // A conexão em tempo real só é liberada quando a página sai da pilha (não ao abrir o editor por cima).
        protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            if (!Navigation.NavigationStack.Contains(this))
                await _vm.StopAsync();
        }
    }
}
