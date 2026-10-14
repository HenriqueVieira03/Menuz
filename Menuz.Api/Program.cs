using Menuz.Api.Data;
using Menuz.Api.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MenuzDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplica migrations e popula o cardápio de demonstração (ambiente de desenvolvimento).
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

app.MapControllers();
app.MapHub<OrderHub>("/hubs/orders");

app.Run();
