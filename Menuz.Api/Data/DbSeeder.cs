using Menuz.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Menuz.Api.Data;

/// <summary>Popula o banco com um cardápio de demonstração (somente se estiver vazio).</summary>
public static class DbSeeder
{
    // Imagens de placeholder; troque por fotos reais (ex.: servidas em wwwroot/images).
    private static string Img(string seed) => $"https://picsum.photos/seed/{seed}/480/320";

    public static async Task SeedAsync(MenuzDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        var lanches = new Category { Name = "Lanches", DisplayOrder = 1 };
        var pizzas = new Category { Name = "Pizzas", DisplayOrder = 2 };
        var bebidas = new Category { Name = "Bebidas", DisplayOrder = 3 };
        var sobremesas = new Category { Name = "Sobremesas", DisplayOrder = 4 };

        lanches.Products.AddRange(new[]
        {
            new Product
            {
                Name = "X-Burger", Description = "Hambúrguer artesanal 150g, queijo, alface, tomate e molho especial.",
                BasePrice = 24.90m, ImageUrl = Img("xburger"),
                Sizes = { new() { Name = "Simples", PriceDelta = 0 }, new() { Name = "Duplo", PriceDelta = 8m } },
                Addons = { new() { Name = "Bacon", Price = 5m }, new() { Name = "Queijo extra", Price = 3.5m }, new() { Name = "Ovo", Price = 2.5m } },
                Ingredients = { new() { Name = "Cebola" }, new() { Name = "Tomate" }, new() { Name = "Alface" }, new() { Name = "Molho especial" } }
            },
            new Product
            {
                Name = "X-Frango", Description = "Filé de frango grelhado, queijo, alface e maionese temperada.",
                BasePrice = 22.90m, ImageUrl = Img("xfrango"),
                Sizes = { new() { Name = "Simples", PriceDelta = 0 }, new() { Name = "Duplo", PriceDelta = 7m } },
                Addons = { new() { Name = "Bacon", Price = 5m }, new() { Name = "Cheddar", Price = 4m } },
                Ingredients = { new() { Name = "Alface" }, new() { Name = "Maionese" } }
            },
            new Product
            {
                Name = "X-Salada", Description = "Hambúrguer, queijo, alface, tomate e cebola roxa.",
                BasePrice = 20.00m, ImageUrl = Img("xsalada"),
                Sizes = { new() { Name = "Simples", PriceDelta = 0 } },
                Addons = { new() { Name = "Ovo", Price = 2.5m } },
                Ingredients = { new() { Name = "Cebola roxa" }, new() { Name = "Tomate" }, new() { Name = "Alface" } }
            }
        });

        pizzas.Products.AddRange(new[]
        {
            new Product
            {
                Name = "Pizza Margherita", Description = "Molho de tomate, mussarela, manjericão fresco e azeite.",
                BasePrice = 39.90m, ImageUrl = Img("margherita"),
                Sizes = { new() { Name = "Média", PriceDelta = 0 }, new() { Name = "Grande", PriceDelta = 12m }, new() { Name = "Família", PriceDelta = 22m } },
                Addons = { new() { Name = "Borda recheada", Price = 9m } },
                Ingredients = { new() { Name = "Manjericão" } }
            },
            new Product
            {
                Name = "Pizza Calabresa", Description = "Calabresa fatiada, cebola, mussarela e orégano.",
                BasePrice = 42.90m, ImageUrl = Img("calabresa"),
                Sizes = { new() { Name = "Média", PriceDelta = 0 }, new() { Name = "Grande", PriceDelta = 12m }, new() { Name = "Família", PriceDelta = 22m } },
                Addons = { new() { Name = "Borda recheada", Price = 9m } },
                Ingredients = { new() { Name = "Cebola" }, new() { Name = "Orégano" } }
            },
            new Product
            {
                Name = "Pizza Frango com Catupiry", Description = "Frango desfiado, catupiry, mussarela e milho.",
                BasePrice = 45.90m, ImageUrl = Img("frangocatupiry"),
                Sizes = { new() { Name = "Média", PriceDelta = 0 }, new() { Name = "Grande", PriceDelta = 12m }, new() { Name = "Família", PriceDelta = 22m } },
                Addons = { new() { Name = "Borda recheada", Price = 9m } },
                Ingredients = { new() { Name = "Milho" } }
            }
        });

        bebidas.Products.AddRange(new[]
        {
            new Product { Name = "Refrigerante Lata", Description = "350ml, gelado.", BasePrice = 6.50m, ImageUrl = Img("refri") },
            new Product
            {
                Name = "Suco Natural", Description = "Laranja, limão ou abacaxi, feito na hora.",
                BasePrice = 9.90m, ImageUrl = Img("suco"),
                Sizes = { new() { Name = "300ml", PriceDelta = 0 }, new() { Name = "500ml", PriceDelta = 4m } },
                Addons = { new() { Name = "Sem açúcar", Price = 0 } }
            },
            new Product { Name = "Água Mineral", Description = "500ml, com ou sem gás.", BasePrice = 4.00m, ImageUrl = Img("agua") }
        });

        sobremesas.Products.AddRange(new[]
        {
            new Product { Name = "Pudim", Description = "Pudim de leite condensado com calda de caramelo.", BasePrice = 12.00m, ImageUrl = Img("pudim") },
            new Product
            {
                Name = "Brownie", Description = "Brownie de chocolate meio amargo, servido morno.",
                BasePrice = 14.90m, ImageUrl = Img("brownie"),
                Addons = { new() { Name = "Bola de sorvete", Price = 5m } }
            }
        });

        db.Categories.AddRange(lanches, pizzas, bebidas, sobremesas);
        await db.SaveChangesAsync();
    }
}
