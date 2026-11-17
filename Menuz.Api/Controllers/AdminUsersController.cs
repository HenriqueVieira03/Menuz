using System.Security.Claims;
using System.Text.RegularExpressions;
using Menuz.Api.Data;
using Menuz.Api.Dtos;
using Menuz.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menuz.Api.Controllers;

/// <summary>
/// Gestão dos usuários da equipe (cozinha e gerência) pela tela do gerente. Não há exclusão:
/// quem sai da equipe é desativado, o que mantém o histórico e bloqueia o acesso na hora.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Manager)]
public class AdminUsersController(MenuzDbContext db) : ControllerBase
{
    private const int MinPasswordLength = 6;

    private static readonly PasswordHasher<User> Hasher = new();

    // Mesmo padrão dos usuários de demonstração: minúsculas, números, ponto, hífen e sublinhado.
    private static readonly Regex UsernamePattern = new("^[a-z0-9._-]{3,40}$", RegexOptions.Compiled);

    [HttpGet]
    public async Task<ActionResult<List<UserAdminDto>>> GetAll(CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking()
            .OrderByDescending(u => u.Role)   // gerentes primeiro
            .ThenBy(u => u.DisplayName)
            .ToListAsync(ct);

        return users.Select(ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<UserAdminDto>> Create(SaveUserRequest request, CancellationToken ct)
    {
        var username = (request.Username ?? string.Empty).Trim().ToLowerInvariant();
        if (!UsernamePattern.IsMatch(username))
            return BadRequest("O usuário deve ter de 3 a 40 caracteres: letras minúsculas, números, ponto, hífen ou sublinhado.");

        var error = ValidateDisplayName(request.DisplayName) ?? ValidatePassword(request.Password, required: true);
        if (error is not null) return BadRequest(error);

        if (!TryParseRole(request.Role, out var role))
            return BadRequest("Perfil inválido. Use Cozinha ou Gerência.");

        if (await db.Users.AnyAsync(u => u.Username == username, ct))
            return Conflict("Já existe um usuário com esse nome de acesso.");

        var user = new User
        {
            Username = username,
            DisplayName = request.DisplayName!.Trim(),
            Role = role,
            IsActive = request.IsActive
        };
        user.PasswordHash = Hasher.HashPassword(user, request.Password!);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return ToDto(user);
    }

    /// <summary>Edita nome, perfil e situação (ativo/desativado) e, se vier senha, redefine a senha.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserAdminDto>> Update(int id, SaveUserRequest request, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound("Usuário não encontrado.");

        var error = ValidateDisplayName(request.DisplayName) ?? ValidatePassword(request.Password, required: false);
        if (error is not null) return BadRequest(error);

        if (!TryParseRole(request.Role, out var role))
            return BadRequest("Perfil inválido. Use Cozinha ou Gerência.");

        var keepsManagerAccess = request.IsActive && role == UserRole.Manager;

        // Ninguém tira o próprio acesso de gerente (evita ficar trancado para fora do painel).
        var currentId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var n) ? n : 0;
        if (user.Id == currentId && !keepsManagerAccess)
            return Conflict("Você não pode desativar nem rebaixar a sua própria conta.");

        // Sempre deve sobrar pelo menos um gerente ativo.
        if (user is { Role: UserRole.Manager, IsActive: true } && !keepsManagerAccess)
        {
            var otherManagers = await db.Users.CountAsync(
                u => u.Id != user.Id && u.Role == UserRole.Manager && u.IsActive, ct);
            if (otherManagers == 0)
                return Conflict("Precisa existir pelo menos um gerente ativo.");
        }

        user.DisplayName = request.DisplayName!.Trim();
        user.Role = role;
        user.IsActive = request.IsActive;
        if (!string.IsNullOrEmpty(request.Password))
            user.PasswordHash = Hasher.HashPassword(user, request.Password);

        await db.SaveChangesAsync(ct);
        return ToDto(user);
    }

    private static string? ValidateDisplayName(string? displayName)
    {
        var name = displayName?.Trim() ?? string.Empty;
        return name.Length is 0 or > 80 ? "Informe o nome de exibição (até 80 caracteres)." : null;
    }

    private static string? ValidatePassword(string? password, bool required)
    {
        if (string.IsNullOrEmpty(password))
            return required ? "Informe uma senha." : null;

        return password.Length < MinPasswordLength
            ? $"A senha precisa ter pelo menos {MinPasswordLength} caracteres."
            : null;
    }

    private static bool TryParseRole(string? text, out UserRole role) =>
        Enum.TryParse(text, ignoreCase: true, out role) && Enum.IsDefined(role);

    private static UserAdminDto ToDto(User u) => new(
        u.Id,
        u.Username,
        u.DisplayName,
        u.Role.ToString(),
        u.IsActive,
        DateTime.SpecifyKind(u.CreatedAt, DateTimeKind.Utc));
}
