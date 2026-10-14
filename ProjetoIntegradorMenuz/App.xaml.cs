using System.Globalization;

namespace ProjetoIntegradorMenuz
{
    public partial class App : Application
    {
        public App()
        {
            // Garante "R$ 24,90" nos StringFormat dos bindings, independente do idioma do dispositivo.
            var ptBr = new CultureInfo("pt-BR");
            CultureInfo.DefaultThreadCurrentCulture = ptBr;
            CultureInfo.DefaultThreadCurrentUICulture = ptBr;
            CultureInfo.CurrentCulture = ptBr;

            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
