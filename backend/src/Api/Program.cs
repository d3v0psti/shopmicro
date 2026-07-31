using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------
// Configuração agnóstica de ambiente (12-factor):
// Connection string via variável de ambiente (docker-compose, K8s, etc).
// ----------------------------------------------------------------------
var connectionString =
    Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string não configurada (DB_CONNECTION_STRING).");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

// Configura o ASP.NET para transformar todas as rotas em minúsculas (evita 404 por Case Sensitivity)
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres");

// CORS configurável por variável de ambiente
var allowedOrigins = (Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS") ?? "*")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Contains("*"))
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        else
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Cria o banco e as tabelas automaticamente e popula dados iniciais
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        
        // Cria o banco e as tabelas com base nos Models, sem precisar de Migrations
        db.Database.EnsureCreated();

        // Popula produtos padrão caso a tabela esteja vazia
        if (!db.Products.Any())
        {
            db.Products.AddRange(
                new Product { Name = "Fone Bluetooth Pro", Description = "Cancelamento de ruído, 30h de bateria.", Price = 349.90m, Category = "Eletrônicos", StockQuantity = 25, ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=500" },
                new Product { Name = "Teclado Mecânico RGB", Description = "Switches hot-swap, layout ABNT2.", Price = 459.00m, Category = "Eletrônicos", StockQuantity = 15, ImageUrl = "https://images.unsplash.com/photo-1618384887929-16ec33fab9ef?w=500" },
                new Product { Name = "Mochila para Notebook", Description = "Compartimento acolchoado até 15.6\".", Price = 189.90m, Category = "Acessórios", StockQuantity = 40, ImageUrl = "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=500" },
                new Product { Name = "Garrafa Térmica 1L", Description = "Mantém temperatura por até 12h.", Price = 79.90m, Category = "Casa", StockQuantity = 60, ImageUrl = "https://images.unsplash.com/photo-1602143407151-7111542de6e8?w=500" },
                new Product { Name = "Mouse Ergonômico", Description = "Sensor de precisão, uso prolongado.", Price = 129.90m, Category = "Eletrônicos", StockQuantity = 30, ImageUrl = "https://images.unsplash.com/photo-1527814050087-3793815479db?w=500" },
                new Product { Name = "Luminária de Mesa LED", Description = "3 temperaturas de cor, regulável.", Price = 99.90m, Category = "Casa", StockQuantity = 20, ImageUrl = "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?w=500" }
            );
            db.SaveChanges();
        }

        // Garante um usuário inicial de teste se não houver nenhum cadastrado
        if (!db.Users.Any())
        {
            db.Users.Add(new User
            {
                Email = "admin@admin.com",
                PasswordHash = "123456",
                FullName = "Administrador do Sistema"
            });
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocorreu um erro ao criar ou popular o banco de dados.");
    }
}

if (app.Environment.IsDevelopment() || Environment.GetEnvironmentVariable("ENABLE_SWAGGER") == "true")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

// Endpoints de Probes de Saúde
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();