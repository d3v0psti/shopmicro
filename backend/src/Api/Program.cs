using Amazon;
using Amazon.S3;
using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------
// Configuração agnóstica de ambiente (12-factor):
// Connection string via variável de ambiente (docker-compose, K8s, etc).
// ----------------------------------------------------------------------
var connectionString =
    Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string não configurada (DB_CONNECTION_STRING ou DefaultConnection).");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

// Configura o ASP.NET para transformar todas as rotas em minúsculas (evita 404 por Case Sensitivity)
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

// Storage provider selection: local, aws, azure, gcp
var storageProvider = Environment.GetEnvironmentVariable("STORAGE_PROVIDER")?.ToLowerInvariant() ?? "local";

switch (storageProvider)
{
    case "aws":
    {
        var bucket = Environment.GetEnvironmentVariable("AWS_S3_BUCKET") ?? throw new InvalidOperationException("AWS_S3_BUCKET não configurado.");
        var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
        builder.Services.AddSingleton<IAmazonS3>(sp => new AmazonS3Client(RegionEndpoint.GetBySystemName(region)));
        builder.Services.AddSingleton<IStorageService>(sp => new S3StorageService(sp.GetRequiredService<IAmazonS3>(), bucket, region));
        break;
    }
    case "azure":
    {
        var blobConnectionString = Environment.GetEnvironmentVariable("AZURE_BLOB_CONNECTION_STRING") ?? throw new InvalidOperationException("AZURE_BLOB_CONNECTION_STRING não configurado.");
        var containerName = Environment.GetEnvironmentVariable("AZURE_BLOB_CONTAINER") ?? "shopmicro-images";
        builder.Services.AddSingleton<IStorageService>(sp => new AzureBlobStorageService(blobConnectionString, containerName));
        break;
    }
    case "gcp":
    {
        var bucket = Environment.GetEnvironmentVariable("GCP_STORAGE_BUCKET") ?? throw new InvalidOperationException("GCP_STORAGE_BUCKET não configurado.");
        var serviceAccountJson = Environment.GetEnvironmentVariable("GCP_SERVICE_ACCOUNT_JSON");
        builder.Services.AddSingleton<IStorageService>(sp => new GcpStorageService(bucket, serviceAccountJson));
        break;
    }
    default:
    {
        builder.Services.AddSingleton<IStorageService, LocalStorageService>();
        break;
    }
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres");

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "shopmicro_dev_secret_12345";
var signingKey = new SymmetricSecurityKey(SecurityService.GetSigningKeyBytes(jwtSecret));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5)
    };
});

builder.Services.AddAuthorization();

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

        // Popula produtos padrão caso faltem no banco ou ainda não tenham sido adicionados.
        var seededProducts = new[]
        {
            new Product { Name = "Notebook Gamer Pro", Description = "Processador potente, RGB, tela 144Hz.", Price = 4500.00m, Category = "Eletrônicos", StockQuantity = 5, ImageUrl = "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=500" },
            new Product { Name = "Mouse Sem Fio RGB", Description = "Sensor preciso, autonomia prolongada.", Price = 150.00m, Category = "Eletrônicos", StockQuantity = 12, ImageUrl = "https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?w=500" },
            new Product { Name = "Teclado Mecânico Switch Blue", Description = "Teclas táteis e duráveis.", Price = 320.00m, Category = "Eletrônicos", StockQuantity = 8, ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=500" },
            new Product { Name = "Monitor Ultrawide 29\"", Description = "Ideal para produtividade e jogos.", Price = 1250.00m, Category = "Eletrônicos", StockQuantity = 3, ImageUrl = "https://images.unsplash.com/photo-1527443224154-c4a3942d3acf?w=500" },
            new Product { Name = "Headset Surround 7.1", Description = "Áudio imersivo e confortável.", Price = 280.00m, Category = "Eletrônicos", StockQuantity = 15, ImageUrl = "https://images.unsplash.com/photo-1546435770-a3e426bf472b?w=500" }
        };

        var existingProductNames = db.Products.Select(p => p.Name).ToHashSet();
        var missingProducts = seededProducts.Where(p => !existingProductNames.Contains(p.Name)).ToArray();
        if (missingProducts.Any())
        {
            db.Products.AddRange(missingProducts);
            db.SaveChanges();
        }

        // Garante um usuário inicial de teste se não houver nenhum cadastrado
        if (!db.Users.Any())
        {
            db.Users.Add(new User
            {
                Email = "admin@admin.com",
                PasswordHash = SecurityService.HashPassword("123456"),
                FullName = "Administrador do Sistema"
            });
            db.SaveChanges();
        }
        else
        {
            // Caso algum usuário legado tenha o PasswordHash armazenado em texto puro,
            // re-hash para permitir login sem travar a aplicação.
            var legacyUser = db.Users.FirstOrDefault(u => !u.PasswordHash.StartsWith("$pbkdf2-sha256$"));
            if (legacyUser != null)
            {
                legacyUser.PasswordHash = SecurityService.HashPassword(legacyUser.PasswordHash);
                db.SaveChanges();
            }
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
app.UseAuthentication();
app.UseAuthorization();

var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.MapControllers();

// Endpoints de Probes de Saúde
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();