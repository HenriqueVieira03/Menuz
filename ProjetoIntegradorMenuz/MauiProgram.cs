using Microsoft.Extensions.Logging;
using ProjetoIntegradorMenuz.Services;
using ProjetoIntegradorMenuz.ViewModels;
using ProjetoIntegradorMenuz.Views;

namespace ProjetoIntegradorMenuz
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Campos de texto sem moldura nativa: a moldura é o Border com estilo MzInput.
            Controls.InputStyling.Apply();

            // Serviços
            builder.Services.AddSingleton(new HttpClient
            {
                BaseAddress = new Uri(ApiConfig.BaseUrl),
                Timeout = TimeSpan.FromSeconds(15)
            });
            builder.Services.AddSingleton<MenuApiService>();
            builder.Services.AddSingleton<CartService>();
            builder.Services.AddSingleton<OrderHubClient>();
            builder.Services.AddSingleton<TableService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<AdminApiService>();

            // ViewModels
            builder.Services.AddSingleton<MenuViewModel>();
            builder.Services.AddTransient<CartViewModel>();
            builder.Services.AddTransient<MyOrdersViewModel>();
            builder.Services.AddTransient<CloseBillViewModel>();

            // Views
            builder.Services.AddSingleton<MenuPage>();
            builder.Services.AddTransient<CartPage>();
            builder.Services.AddTransient<MyOrdersPage>();
            builder.Services.AddTransient<CloseBillPage>();

            // Equipe: cozinha (KDS) e gerência (PDV), com login
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<KitchenViewModel>();
            builder.Services.AddTransient<KitchenPage>();
            builder.Services.AddTransient<ManagerViewModel>();
            builder.Services.AddTransient<ManagerPage>();
            builder.Services.AddTransient<ProductEditViewModel>();
            builder.Services.AddTransient<ProductEditPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
