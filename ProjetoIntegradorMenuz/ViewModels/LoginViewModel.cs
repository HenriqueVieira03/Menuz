using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoIntegradorMenuz.Models;
using ProjetoIntegradorMenuz.Services;
using ProjetoIntegradorMenuz.Views;

namespace ProjetoIntegradorMenuz.ViewModels
{
    /// <summary>Login da equipe. Depois de entrar, cada perfil vai para a sua tela.</summary>
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthService _auth;

        public LoginViewModel(AuthService auth) => _auth = auth;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string username = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string password = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private bool isBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? errorMessage;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        private bool CanLogin() => !IsBusy && Username.Trim().Length > 0 && Password.Length > 0;

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync()
        {
            IsBusy = true;
            ErrorMessage = null;
            try
            {
                ErrorMessage = await _auth.LoginAsync(Username, Password);
                if (ErrorMessage is not null) return;

                Password = string.Empty;

                // "../" troca a tela de login pela tela do perfil: voltar leva ao cardápio (e encerra a sessão).
                var target = _auth.IsManager ? nameof(ManagerPage) : nameof(KitchenPage);
                await Shell.Current.GoToAsync($"../{target}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task CancelAsync() => await Shell.Current.GoToAsync("..");
    }
}
