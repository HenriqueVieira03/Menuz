using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoIntegradorMenuz.Models;
using ProjetoIntegradorMenuz.Services;

namespace ProjetoIntegradorMenuz.ViewModels
{
    /// <summary>Cadastro e edição de usuário da equipe (cozinha ou gerência). Recebe "?id=0" para novo ou "?id=N".</summary>
    public partial class UserEditViewModel : ObservableObject, IQueryAttributable
    {
        private const int MinPasswordLength = 6;
        private static readonly Regex UsernamePattern = new("^[a-z0-9._-]{3,40}$");

        private readonly AdminApiService _admin;
        private int _id;

        public UserEditViewModel(AdminApiService admin) => _admin = admin;

        public IReadOnlyList<RoleOption> Roles => RoleOption.All;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNew))]
        [NotifyPropertyChangedFor(nameof(IsEditing))]
        [NotifyPropertyChangedFor(nameof(PageTitle))]
        [NotifyPropertyChangedFor(nameof(SaveText))]
        [NotifyPropertyChangedFor(nameof(PasswordLabel))]
        [NotifyPropertyChangedFor(nameof(PasswordPlaceholder))]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private bool isLoaded;

        [ObservableProperty] private string username = string.Empty;
        [ObservableProperty] private string displayName = string.Empty;
        [ObservableProperty] private string password = string.Empty;
        [ObservableProperty] private string confirmPassword = string.Empty;
        [ObservableProperty] private RoleOption selectedRole = RoleOption.Kitchen;
        [ObservableProperty] private bool isActive = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private bool isBusy;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        private string? errorMessage;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // _id == 0: cadastro novo. O nome de acesso só é digitado no cadastro; depois não muda.
        public bool IsNew => _id == 0;
        public bool IsEditing => !IsNew;

        public string PageTitle => IsNew ? "Novo usuário" : "Editar usuário";
        public string SaveText => IsNew ? "Criar usuário" : "Salvar alterações";
        public string PasswordLabel => IsNew ? "Senha *" : "Nova senha";

        public string PasswordPlaceholder => IsNew
            ? $"Mínimo de {MinPasswordLength} caracteres"
            : "Deixe em branco para manter a senha atual";

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _id = query.TryGetValue("id", out var raw) && int.TryParse(raw?.ToString(), out var id) ? id : 0;
            OnPropertyChanged(nameof(IsNew));
            OnPropertyChanged(nameof(IsEditing));
            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(SaveText));
            OnPropertyChanged(nameof(PasswordLabel));
            OnPropertyChanged(nameof(PasswordPlaceholder));
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            if (IsNew)
            {
                IsLoaded = true;
                return;
            }

            await RunAsync(async () =>
            {
                var users = await _admin.GetUsersAsync();
                var user = users.FirstOrDefault(u => u.Id == _id)
                           ?? throw new OrderRejectedException("Usuário não encontrado.");

                Username = user.Username;
                DisplayName = user.DisplayName;
                SelectedRole = RoleOption.From(user.Role);
                IsActive = user.IsActive;
                IsLoaded = true;
            });
        }

        private bool CanSave() => IsLoaded && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            var error = Validate();
            if (error is not null)
            {
                ErrorMessage = error;
                return;
            }

            var request = new SaveUserRequest
            {
                Username = Username.Trim().ToLowerInvariant(),
                DisplayName = DisplayName.Trim(),
                Password = string.IsNullOrEmpty(Password) ? null : Password,
                Role = SelectedRole.Value,
                IsActive = IsActive
            };

            var ok = await RunAsync(async () =>
            {
                if (IsNew) await _admin.CreateUserAsync(request);
                else await _admin.UpdateUserAsync(_id, request);
            });

            if (ok) await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task CancelAsync() => await Shell.Current.GoToAsync("..");

        private string? Validate()
        {
            if (IsNew && !UsernamePattern.IsMatch(Username.Trim().ToLowerInvariant()))
                return "O usuário deve ter de 3 a 40 caracteres: letras minúsculas, números, ponto, hífen ou sublinhado.";

            if (string.IsNullOrWhiteSpace(DisplayName))
                return "Informe o nome de exibição.";

            if (IsNew && string.IsNullOrEmpty(Password))
                return "Informe uma senha.";

            if (!string.IsNullOrEmpty(Password))
            {
                if (Password.Length < MinPasswordLength)
                    return $"A senha precisa ter pelo menos {MinPasswordLength} caracteres.";
                if (Password != ConfirmPassword)
                    return "A confirmação da senha não confere.";
            }

            return null;
        }

        private async Task<bool> RunAsync(Func<Task> action)
        {
            IsBusy = true;
            ErrorMessage = null;
            try
            {
                await action();
                return true;
            }
            catch (Exception ex) when (ex is StaffAccessException or OrderRejectedException)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                ErrorMessage = "Não foi possível falar com o servidor. Verifique se a API está em execução.";
            }
            finally
            {
                IsBusy = false;
            }
            return false;
        }
    }
}
