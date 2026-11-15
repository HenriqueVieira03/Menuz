using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Menuz.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace Menuz.Api.Services;

/// <summary>Configuração do JWT (seção "Jwt" do appsettings).</summary>
public class JwtOptions
{
    public string Issuer { get; set; } = "Menuz.Api";
    public string Audience { get; set; } = "Menuz.App";

    /// <summary>Chave simétrica (mínimo 32 caracteres). Em produção, definir por variável de ambiente Jwt__Key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Validade do token. Um turno de trabalho cabe com folga.</summary>
    public int ExpirationHours { get; set; } = 12;

    public SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(Key));
}

public class TokenService(JwtOptions options)
{
    public (string Token, DateTime ExpiresAt) Create(User user)
    {
        var expires = DateTime.UtcNow.AddHours(options.ExpirationHours);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("display_name", user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(options.SigningKey(), SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
