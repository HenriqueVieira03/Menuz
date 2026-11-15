using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.Services
{
    /// <summary>
    /// Sessão da equipe (cozinha/gerência). O token fica só na memória: ao voltar para o cardápio
    /// do cliente ou fechar o app a sessão acaba, o que é o comportamento esperado num tablet compartilhado.
    /// </summary>
    public partial class AuthService : ObservableObject
    {
        private readonly HttpClient _http;

        public AuthService(HttpClient http) => _http = http;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLoggedIn))]
        [NotifyPropertyChangedFor(nameof(IsManager))]
        private LoginResponse? session;

        public bool IsLoggedIn => Session is not null && Session.ExpiresAt > DateTime.UtcNow;
        public bool IsManager => IsLoggedIn && Session!.Role == StaffRoles.Manager;
        public string? Token => IsLoggedIn ? Session!.Token : null;
        public string DisplayName => Session?.DisplayName ?? string.Empty;

        /// <summary>Disparado ao sair: quem mantém conexões abertas com o token (SignalR) deve fechá-las.</summary>
        public event Action? LoggedOut;

        /// <summary>Retorna null se deu certo, ou a mensagem para mostrar na tela de login.</summary>
        public async Task<string?> LoginAsync(string username, string password, CancellationToken ct = default)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/auth/login",
                    new LoginRequest { Username = username.Trim(), Password = password }, ct);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return "Usuário ou senha inválidos.";

                response.EnsureSuccessStatusCode();

                var result = (await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct))!;
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
                Session = result;
                return null;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return "Não foi possível conectar ao servidor. Verifique se a API está em execução.";
            }
        }

        public void Logout()
        {
            if (Session is null) return;

            _http.DefaultRequestHeaders.Authorization = null;
            Session = null;
            LoggedOut?.Invoke();
        }
    }

    /// <summary>A API respondeu 401/403: sessão expirada ou perfil sem permissão.</summary>
    public class StaffAccessException(string message) : Exception(message);
}
