using Menuz.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Menuz.Api.Data;

public class MenuzDbContext(DbContextOptions<MenuzDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSize> ProductSizes => Set<ProductSize>();
    public DbSet<ProductAddon> ProductAddons => Set<ProductAddon>();
    public DbSet<ProductIngredient> ProductIngredients => Set<ProductIngredient>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Category>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        mb.Entity<Product>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.BasePrice).HasPrecision(10, 2);
            e.HasOne(x => x.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<ProductSize>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(60).IsRequired();
            e.Property(x => x.PriceDelta).HasPrecision(10, 2);
            e.HasOne<Product>().WithMany(p => p.Sizes)
             .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ProductAddon>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(60).IsRequired();
            e.Property(x => x.Price).HasPrecision(10, 2);
            e.HasOne<Product>().WithMany(p => p.Addons)
             .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<ProductIngredient>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(60).IsRequired();
            e.HasOne<Product>().WithMany(p => p.Ingredients)
             .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Order>(e =>
        {
            e.Property(x => x.Total).HasPrecision(10, 2);
            e.HasIndex(x => x.Status);
            e.HasMany(x => x.Items).WithOne()
             .HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<OrderItem>(e =>
        {
            e.Property(x => x.ProductName).HasMaxLength(120).IsRequired();
            e.Property(x => x.UnitPrice).HasPrecision(10, 2);
        });
    }
}
