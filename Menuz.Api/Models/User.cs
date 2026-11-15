namespace Menuz.Api.Models;

/// <summary>Perfis da equipe. O cliente (tablet da mesa) não faz login.</summary>
public enum UserRole
{
    Kitchen,  // cozinha (KDS): vê pedidos e avança o status
    Manager   // gerência (PDV): tudo da cozinha + cardápio, cancelamentos e indicadores
}

/// <summary>Nomes dos perfis usados em [Authorize(Roles = ...)] e no token.</summary>
public static class Roles
{
    public const string Kitchen = nameof(UserRole.Kitchen);
    public const string Manager = nameof(UserRole.Manager);
    public const string Staff = Kitchen + "," + Manager;
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Hash gerado pelo PasswordHasher do ASP.NET Core Identity (PBKDF2 com salt). A senha nunca é gravada.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
