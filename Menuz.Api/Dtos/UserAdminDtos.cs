namespace Menuz.Api.Dtos;

/// <summary>Usuário da equipe para a tela de gestão. Nunca inclui a senha nem o hash.</summary>
public record UserAdminDto(
    int Id,
    string Username,
    string DisplayName,
    string Role,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>
/// Criação (Username e Password obrigatórios) e edição (Username é ignorado; Password só se for redefinir).
/// Role é "Kitchen" ou "Manager".
/// </summary>
public record SaveUserRequest(
    string? Username,
    string? DisplayName,
    string? Password,
    string? Role,
    bool IsActive = true);
