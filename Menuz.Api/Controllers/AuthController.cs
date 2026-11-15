using System.Security.Claims;
using Menuz.Api.Data;
using Menuz.Api.Dtos;
using Menuz.Api.Models;
using Menuz.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menuz.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(MenuzDbContext db, TokenService tokens, ILogger<AuthController> logger) : ControllerBase
{
    private static readonly PasswordHasher<User> Hasher = new();

    /// <summary>Login da equipe (cozinha/gerência). Retorna o token JWT a ser enviado em Authorization: Bearer.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var username = request.Username?.Trim().ToLowerInvariant() ?? string.Empty;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct);

        // Mesma resposta para usuário inexistente e senha errada: não revela quais usuários existem.
        if (user is null || string.IsNullOrEmpty(request.Password) ||
            Hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Falha de login para o usuário {Username}", username);
            return Unauthorized("Usuário ou senha inválidos.");
        }

        var (token, expiresAt) = tokens.Create(user);
        return new LoginResponse(token, expiresAt, user.Username, user.DisplayName, user.Role.ToString());
    }

    /// <summary>Confere se o token ainda é válido e devolve quem está logado.</summary>
    [HttpGet("me")]
    [Authorize]
    public ActionResult<MeResponse> Me() => new MeResponse(
        User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
        User.FindFirstValue("display_name") ?? string.Empty,
        User.FindFirstValue(ClaimTypes.Role) ?? string.Empty);
}
