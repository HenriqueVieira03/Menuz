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

            // Serviços
            builder.Services.AddSingleton(new HttpClient
            {
                BaseAddress = new Uri(ApiConfig.BaseUrl),
                Timeout = TimeSpan.FromSeconds(15)
            });
            builder.Services.AddSingleton<MenuApiService>();
            builder.Services.AddSingleton<CartService>();

            // ViewModels
            builder.Services.AddSingleton<MenuViewModel>();
            builder.Services.AddTransient<CartViewModel>();

            // Views
            builder.Services.AddSingleton<MenuPage>();
            builder.Services.AddTransient<CartPage>();

            builder.Services.AddTransient<KitchenViewModel>();
            builder.Services.AddTransient<KitchenPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
