namespace Menuz.Api.Models;

/// <summary>
/// Configurações do estabelecimento (linha única, Id = 1): nome exibido no app, cor de destaque e tema.
/// A gerência altera na tela "Aparência"; todos os tablets e telas da equipe aplicam ao abrir.
/// </summary>
public class RestaurantSettings
{
    public const int SingletonId = 1;

    public static readonly string[] AccentKeys = { "menuz", "tomate", "oliva", "petroleo", "vinho", "cacau" };
    public static readonly string[] ThemeModes = { "Light", "Dark" };

    public int Id { get; set; } = SingletonId;
    public string RestaurantName { get; set; } = "Menuz";
    public string AccentKey { get; set; } = "menuz";
    public string ThemeMode { get; set; } = "Light";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
