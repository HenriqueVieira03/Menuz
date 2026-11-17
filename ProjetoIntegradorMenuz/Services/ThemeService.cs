using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using ProjetoIntegradorMenuz.Models;

namespace ProjetoIntegradorMenuz.Services
{
    /// <summary>Cor de destaque do app (botões, seleção, preços). A neutra (fundo creme e texto marrom) é sempre a do Menuz.</summary>
    public record AccentPreset(string Key, string Name, Color Primary, Color Strong, Color SoftLight, Color SoftDark, Color PrimaryOnDark);

    public enum AppThemeMode { Light, Dark }

    /// <summary>
    /// Aparência do app: nome do estabelecimento, cor de destaque e tema claro/escuro.
    /// A gerência escolhe na tela "Aparência"; a escolha fica no servidor e vale para todos os aparelhos.
    /// Todas as telas usam as cores por DynamicResource (tokens Mz*), então a troca acontece na hora, sem reiniciar.
    /// </summary>
    public partial class ThemeService : ObservableObject
    {
        private const string CacheKey = "menuz.appearance";
        public const string DefaultName = "Menuz";

        public static readonly IReadOnlyList<AccentPreset> Presets = new[]
        {
            new AccentPreset("menuz", "Laranja Menuz", Color.FromArgb("#F77F00"), Color.FromArgb("#C25F00"), Color.FromArgb("#FFF1E0"), Color.FromArgb("#3A2212"), Color.FromArgb("#FF8C1A")),
            new AccentPreset("tomate", "Tomate", Color.FromArgb("#D64527"), Color.FromArgb("#A8321A"), Color.FromArgb("#FCEAE5"), Color.FromArgb("#3B1A14"), Color.FromArgb("#F06A4D")),
            new AccentPreset("oliva", "Oliva", Color.FromArgb("#5E8C31"), Color.FromArgb("#46692A"), Color.FromArgb("#EDF3E4"), Color.FromArgb("#22301A"), Color.FromArgb("#86B656")),
            new AccentPreset("petroleo", "Azul petróleo", Color.FromArgb("#1F6F8B"), Color.FromArgb("#17556B"), Color.FromArgb("#E4F0F4"), Color.FromArgb("#12262E"), Color.FromArgb("#4AA3C4")),
            new AccentPreset("vinho", "Vinho", Color.FromArgb("#8E2C48"), Color.FromArgb("#6E2138"), Color.FromArgb("#F6E6EB"), Color.FromArgb("#301820"), Color.FromArgb("#C95B80")),
            new AccentPreset("cacau", "Cacau", Color.FromArgb("#7A4A2E"), Color.FromArgb("#5C3721"), Color.FromArgb("#F3E9E1"), Color.FromArgb("#2C1E16"), Color.FromArgb("#C08A64")),
        };

        private readonly HttpClient _http;

        public ThemeService(HttpClient http)
        {
            _http = http;
            var cached = Preferences.Default.Get(CacheKey, string.Empty);
            Current = Parse(cached) ?? new Appearance();
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RestaurantName))]
        [NotifyPropertyChangedFor(nameof(Initial))]
        private Appearance current;

        public string RestaurantName => string.IsNullOrWhiteSpace(Current.RestaurantName) ? DefaultName : Current.RestaurantName;

        /// <summary>Letra do logotipo (primeira letra do nome).</summary>
        public string Initial => RestaurantName[..1].ToUpperInvariant();

        public AccentPreset Accent => Presets.FirstOrDefault(p => p.Key == Current.AccentKey) ?? Presets[0];
        public AppThemeMode Mode => Current.ThemeMode == "Dark" ? AppThemeMode.Dark : AppThemeMode.Light;

        /// <summary>Aplica a aparência salva no aparelho (chamado na abertura, antes da primeira tela).</summary>
        public void ApplyCached() => Apply(Current);

        /// <summary>Busca a aparência no servidor e aplica se mudou. Falhas de rede mantêm a atual.</summary>
        public async Task RefreshFromServerAsync()
        {
            try
            {
                var server = await _http.GetFromJsonAsync<Appearance>("api/settings/appearance");
                if (server is not null) Apply(server);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                System.Diagnostics.Debug.WriteLine($"Aparência: usando a salva no aparelho ({ex.Message}).");
            }
        }

        /// <summary>Aplica e guarda no aparelho (para a próxima abertura já vir certa, mesmo offline).</summary>
        public void Apply(Appearance appearance)
        {
            Current = appearance;
            Preferences.Default.Set(CacheKey, System.Text.Json.JsonSerializer.Serialize(appearance));
            OnPropertyChanged(nameof(Accent));
            OnPropertyChanged(nameof(Mode));

            if (Application.Current is not { } app) return;
            if (MainThread.IsMainThread) ApplyTokens(app);
            else MainThread.BeginInvokeOnMainThread(() => ApplyTokens(app));
        }

        /// <summary>Pré-visualização: aplica sem guardar (a tela de Aparência usa enquanto o gerente escolhe).</summary>
        public void Preview(string accentKey, AppThemeMode mode)
        {
            if (Application.Current is { } app)
                ApplyTokens(app, Presets.FirstOrDefault(p => p.Key == accentKey) ?? Presets[0], mode);
        }

        private void ApplyTokens(Application app) => ApplyTokens(app, Accent, Mode);

        private static void ApplyTokens(Application app, AccentPreset accent, AppThemeMode mode)
        {
            var dark = mode == AppThemeMode.Dark;
            var r = app.Resources;

            // Neutros: paleta Menuz (creme/marrom). Não mudam com a cor de destaque.
            r["MzBackground"] = C(dark ? "#1A0E0A" : "#F4F4EB");
            r["MzSurface"] = C(dark ? "#241611" : "#FFFFFF");
            r["MzSurfaceAlt"] = C(dark ? "#33221A" : "#EFE7DC");
            r["MzSurfaceSunken"] = C(dark ? "#1F130E" : "#F8F5EE");
            r["MzBorder"] = C(dark ? "#3A261F" : "#E6D9CC");
            r["MzBorderStrong"] = C(dark ? "#4D3429" : "#D6C3B2");
            r["MzText"] = C(dark ? "#F4F4EB" : "#532718");
            r["MzTextMuted"] = C(dark ? "#CBB7AA" : "#6B4F43");
            r["MzTextSubtle"] = C(dark ? "#8C776B" : "#A08B80");

            // Barra lateral da equipe (marrom do Menuz)
            r["MzSidebar"] = C(dark ? "#120A07" : "#3B1C11");
            r["MzSidebarText"] = C("#F4EDE6");
            r["MzSidebarMuted"] = C(dark ? "#9C877B" : "#C9B3A6");
            r["MzSidebarActive"] = C("#26FFFFFF");

            // Destaque
            r["MzPrimary"] = dark ? accent.PrimaryOnDark : accent.Primary;
            r["MzPrimaryStrong"] = dark ? accent.PrimaryOnDark : accent.Strong;
            r["MzPrimarySoft"] = dark ? accent.SoftDark : accent.SoftLight;
            r["MzOnPrimary"] = C(dark ? "#1A0E0A" : "#FFFFFF");

            // Estados
            r["MzSuccess"] = C(dark ? "#6BAF6B" : "#4E9A4E");
            r["MzSuccessSoft"] = C(dark ? "#1E2B1E" : "#E8F3E8");
            r["MzSuccessText"] = C(dark ? "#9AD19A" : "#2F6B2F");
            r["MzWarning"] = C("#E19A2B");
            r["MzWarningSoft"] = C(dark ? "#33270F" : "#FBEFD8");
            r["MzWarningText"] = C(dark ? "#EBC071" : "#8A5300");
            r["MzDanger"] = C(dark ? "#FF8A65" : "#C93C20");
            r["MzDangerSoft"] = C(dark ? "#3A1A15" : "#FBE9E5");
            r["MzDangerText"] = C(dark ? "#FF9F80" : "#B3341B");
            r["MzDangerBorder"] = C(dark ? "#5A2A20" : "#F0C9C0");

            app.UserAppTheme = dark ? AppTheme.Dark : AppTheme.Light;
        }

        private static Color C(string hex) => Color.FromArgb(hex);

        private static Appearance? Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return System.Text.Json.JsonSerializer.Deserialize<Appearance>(json); }
            catch (System.Text.Json.JsonException) { return null; }
        }
    }
}
