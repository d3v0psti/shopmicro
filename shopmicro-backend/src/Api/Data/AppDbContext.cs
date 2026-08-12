using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<MarketplaceAccount> MarketplaceAccounts => Set<MarketplaceAccount>();
    public DbSet<AdministrativeAccount> AdministrativeAccounts => Set<AdministrativeAccount>();
    public DbSet<MarketplaceRefreshToken> MarketplaceRefreshTokens => Set<MarketplaceRefreshToken>();
    public DbSet<AdministrativeRefreshToken> AdministrativeRefreshTokens => Set<AdministrativeRefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Description).HasMaxLength(2000);
            entity.Property(p => p.Price).HasColumnType("numeric(10,2)");
            entity.Property(p => p.Category).HasMaxLength(80);
            entity.HasIndex(p => p.Category);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.CustomerName).IsRequired().HasMaxLength(150);
            entity.Property(o => o.CustomerEmail).IsRequired().HasMaxLength(200);
            entity.Property(o => o.CustomerCpf).HasMaxLength(20);
            entity.Property(o => o.CustomerPhone).HasMaxLength(20);
            entity.Property(o => o.CustomerCep).HasMaxLength(10);
            entity.Property(o => o.CustomerAddress).HasMaxLength(300);
            entity.Property(o => o.PaymentMethod).IsRequired().HasMaxLength(30);
            entity.Property(o => o.TotalAmount).HasColumnType("numeric(10,2)");
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(o => o.CustomerEmail);
            entity.HasMany(o => o.Items)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.ProductName).IsRequired().HasMaxLength(150);
            entity.Property(i => i.UnitPrice).HasColumnType("numeric(10,2)");
        });

        modelBuilder.Entity<MarketplaceRefreshToken>(entity =>
        {
            entity.ToTable("marketplace_refresh_tokens");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.TokenHash).IsRequired();
            entity.Property(rt => rt.ExpiresAt).IsRequired();
            entity.Property(rt => rt.Revoked).HasDefaultValue(false);
            entity.HasOne(rt => rt.MarketplaceAccount)
                  .WithMany()
                  .HasForeignKey(rt => rt.MarketplaceAccountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AdministrativeRefreshToken>(entity =>
        {
            entity.ToTable("administrative_refresh_tokens");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.TokenHash).IsRequired();
            entity.Property(rt => rt.ExpiresAt).IsRequired();
            entity.Property(rt => rt.Revoked).HasDefaultValue(false);
            entity.HasOne(rt => rt.AdministrativeAccount)
                  .WithMany()
                  .HasForeignKey(rt => rt.AdministrativeAccountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MarketplaceAccount>(entity =>
        {
            entity.ToTable("marketplace_accounts");
            entity.HasKey(account => account.Id);
            entity.Property(account => account.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(account => account.Email).IsUnique();
            entity.Property(account => account.PasswordHash).IsRequired();
            entity.Property(account => account.FullName).IsRequired().HasMaxLength(150);
            entity.Property(account => account.Cpf).HasMaxLength(20);
            entity.Property(account => account.Phone).HasMaxLength(20);
            entity.Property(account => account.Cep).HasMaxLength(10);
            entity.Property(account => account.Address).HasMaxLength(300);
        });

        modelBuilder.Entity<AdministrativeAccount>(entity =>
        {
            entity.ToTable("administrative_accounts");
            entity.HasKey(account => account.Id);
            entity.Property(account => account.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(account => account.Email).IsUnique();
            entity.Property(account => account.PasswordHash).IsRequired();
            entity.Property(account => account.FullName).IsRequired().HasMaxLength(150);
        });
    }
}
