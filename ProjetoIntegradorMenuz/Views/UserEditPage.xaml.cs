namespace ProjetoIntegradorMenuz.Views
{
    public partial class UserEditPage : ContentPage
    {
        public UserEditPage(ViewModels.UserEditViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
