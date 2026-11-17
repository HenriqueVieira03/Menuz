using System.Globalization;
using ProjetoIntegradorMenuz.Services;

namespace ProjetoIntegradorMenuz
{
    public partial class App : Application
    {
        private readonly ThemeService _theme;

        public App(ThemeService theme)
        {
            // Garante "R$ 24,90" nos StringFormat dos bindings, independente do idioma do dispositivo.
            var ptBr = new CultureInfo("pt-BR");
            CultureInfo.DefaultThreadCurrentCulture = ptBr;
            CultureInfo.DefaultThreadCurrentUICulture = ptBr;
            CultureInfo.CurrentCulture = ptBr;

            InitializeComponent();

            // Aparência salva no aparelho já na primeira tela; a do servidor chega logo depois.
            _theme = theme;
            _theme.ApplyCached();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell()) { Title = _theme.RestaurantName };
            _ = _theme.RefreshFromServerAsync();
            return window;
        }
    }
}
