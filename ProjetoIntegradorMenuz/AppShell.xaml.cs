using ProjetoIntegradorMenuz.Views;

namespace ProjetoIntegradorMenuz
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(CartPage), typeof(CartPage));
            Routing.RegisterRoute(nameof(KitchenPage), typeof(KitchenPage));
        }
    }
}
