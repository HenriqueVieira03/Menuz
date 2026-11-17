using Menuz.Api.Data;
using Menuz.Api.Hubs;
using Menuz.Api.Models;
using Menuz.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MenuzDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ---- Autenticação da equipe (JWT) ----
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < 32)
    throw new InvalidOperationException(
        "Configure Jwt:Key com pelo menos 32 caracteres (appsettings.Development.json ou variável de ambiente Jwt__Key).");

builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<ImageStorage>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // mantém "role", "sub" etc. como estão no token
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = jwt.SigningKey(),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // SignalR (WebSockets) não envia cabeçalhos: o token vem na query string ?access_token=...
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    context.Token = token;
                return Task.CompletedTask;
            },

            // Conta desativada, removida ou com outro perfil depois do login: o token antigo deixa de valer
            // na hora (sem isso ele continuaria aceito até expirar, em até 12 h).
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                var idText = (principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) ?? principal?.FindFirst("sub"))?.Value;
                var roleText = (principal?.FindFirst(System.Security.Claims.ClaimTypes.Role) ?? principal?.FindFirst("role"))?.Value;
                var db = context.HttpContext.RequestServices.GetRequiredService<MenuzDbContext>();

                UserRole? currentRole = null;
                if (int.TryParse(idText, out var userId))
                {
                    currentRole = await db.Users.AsNoTracking()
                        .Where(u => u.Id == userId && u.IsActive)
                        .Select(u => (UserRole?)u.Role)
                        .FirstOrDefaultAsync(context.HttpContext.RequestAborted);
                }

                if (currentRole is null || currentRole.Value.ToString() != roleText)
                    context.Fail("Sessão inválida: usuário desativado ou perfil alterado.");
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Botão "Authorize" no Swagger: cole o token retornado por POST /api/auth/login.
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Token obtido em POST /api/auth/login"
    });
    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", doc)] = new List<string>()
    });
});

var app = builder.Build();

// Aplica migrations e popula o cardápio e os usuários de demonstração (ambiente de desenvolvimento).
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MenuzDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

// Fotos dos produtos enviadas pela gerência (wwwroot/uploads), acessíveis pelo tablet sem login.
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OrderHub>("/hubs/orders");

app.Run();
