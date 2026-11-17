using ProjetoIntegradorMenuz.Views;

namespace ProjetoIntegradorMenuz
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(CartPage), typeof(CartPage));
            Routing.RegisterRoute(nameof(MyOrdersPage), typeof(MyOrdersPage));
            Routing.RegisterRoute(nameof(CloseBillPage), typeof(CloseBillPage));
            Routing.RegisterRoute(nameof(UserEditPage), typeof(UserEditPage));
            Routing.RegisterRoute(nameof(KitchenPage), typeof(KitchenPage));
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(ManagerPage), typeof(ManagerPage));
            Routing.RegisterRoute(nameof(ProductEditPage), typeof(ProductEditPage));
        }
    }
}
