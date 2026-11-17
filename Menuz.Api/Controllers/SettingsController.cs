using Menuz.Api.Data;
using Menuz.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Menuz.Api.Controllers;

public record AppearanceDto(string RestaurantName, string AccentKey, string ThemeMode);

/// <summary>Aparência do estabelecimento: leitura pública (tablets), alteração só pela gerência.</summary>
[ApiController]
[Route("api/settings")]
public class SettingsController(MenuzDbContext db) : ControllerBase
{
    [HttpGet("appearance")]
    [AllowAnonymous]
    public async Task<ActionResult<AppearanceDto>> GetAppearance(CancellationToken ct)
    {
        var s = await db.Settings.AsNoTracking().FirstOrDefaultAsync(ct) ?? new RestaurantSettings();
        return new AppearanceDto(s.RestaurantName, s.AccentKey, s.ThemeMode);
    }

    [HttpPut("appearance")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<ActionResult<AppearanceDto>> UpdateAppearance(AppearanceDto request, CancellationToken ct)
    {
        var name = request.RestaurantName?.Trim() ?? string.Empty;
        if (name.Length is 0 or > 40)
            return BadRequest("O nome do estabelecimento deve ter de 1 a 40 caracteres.");
        if (!RestaurantSettings.AccentKeys.Contains(request.AccentKey))
            return BadRequest("Cor de destaque inválida.");
        if (!RestaurantSettings.ThemeModes.Contains(request.ThemeMode))
            return BadRequest("Tema inválido. Use Light ou Dark.");

        var s = await db.Settings.FirstOrDefaultAsync(ct);
        if (s is null)
        {
            s = new RestaurantSettings();
            db.Settings.Add(s);
        }

        s.RestaurantName = name;
        s.AccentKey = request.AccentKey;
        s.ThemeMode = request.ThemeMode;
        s.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new AppearanceDto(s.RestaurantName, s.AccentKey, s.ThemeMode);
    }
}
