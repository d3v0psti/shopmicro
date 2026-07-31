using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<User> Users => Set<User>(); // <-- ESSA LINHA FALTAVA E CAUSAVA O ERRO

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

        // Configuração da tabela de usuários baseada em E-mail
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(u => u.Email).IsUnique(); // Garante que o e-mail é único no banco
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(150);
        });
    }
}