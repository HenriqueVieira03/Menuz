namespace ProjetoIntegradorMenuz.Models
{
    /// <summary>Aparência do estabelecimento (GET/PUT /api/settings/appearance).</summary>
    public class Appearance
    {
        public string RestaurantName { get; set; } = "Menuz";

        /// <summary>Chave do preset de cor (ver ThemeService.Presets). "menuz" = laranja padrão.</summary>
        public string AccentKey { get; set; } = "menuz";

        /// <summary>"Light" ou "Dark".</summary>
        public string ThemeMode { get; set; } = "Light";
    }
}
