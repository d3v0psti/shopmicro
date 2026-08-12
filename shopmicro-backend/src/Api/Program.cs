using Api.Data;
using Api.Models;
using Api.Security;
using Api.Services;
using Amazon;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// A conexão local é fornecida pelo Docker Compose.
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

var storageProvider = Environment.GetEnvironmentVariable("STORAGE_PROVIDER") ?? "Local";
if (storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
{
    var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION");
    if (string.IsNullOrWhiteSpace(awsRegion))
        throw new InvalidOperationException("AWS_REGION não configurada para o storage S3.");

    builder.Services.AddSingleton<IAmazonS3>(
        _ => new AmazonS3Client(RegionEndpoint.GetBySystemName(awsRegion)));
    builder.Services.AddSingleton<IStorageService, S3StorageService>();
}
else if (storageProvider.Equals("Local", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IStorageService, LocalStorageService>();
}
else
{
    throw new InvalidOperationException(
        $"STORAGE_PROVIDER inválido: '{storageProvider}'. Use Local ou S3.");
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.MarketplaceOnly, policy =>
        policy.RequireRole("Client")
            .RequireClaim(IdentityScopes.ClaimName, IdentityScopes.Marketplace));
    options.AddPolicy(AuthorizationPolicies.AdministrationOnly, policy =>
        policy.RequireRole("Admin")
            .RequireClaim(IdentityScopes.ClaimName, IdentityScopes.Administration));
});

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

// Aplica mudanças de esquema e popula somente dados públicos de demonstração.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        
        db.Database.Migrate();

        // Popula produtos padrão caso faltem no banco ou ainda não tenham sido adicionados.
        var seededProducts = new[]
        {
            new Product { Name = "Notebook Gamer Pro", Description = "Processador potente, RGB, tela 144Hz.", Price = 4500.00m, Category = "Eletrônicos", StockQuantity = 5, ImageUrl = "/assets/products/notebook-generico.png" },
            new Product { Name = "Mouse Sem Fio RGB", Description = "Sensor preciso, autonomia prolongada.", Price = 150.00m, Category = "Eletrônicos", StockQuantity = 12, ImageUrl = "/assets/products/mouse-generico.png" },
            new Product { Name = "Teclado Mecânico Switch Blue", Description = "Teclas táteis e duráveis.", Price = 320.00m, Category = "Eletrônicos", StockQuantity = 8, ImageUrl = "/assets/products/teclado-generico.png" },
            new Product { Name = "Monitor Ultrawide 29\"", Description = "Ideal para produtividade e jogos.", Price = 1250.00m, Category = "Eletrônicos", StockQuantity = 3, ImageUrl = "/assets/products/monitor-generico.png" },
            new Product { Name = "Headset Surround 7.1", Description = "Áudio imersivo e confortável.", Price = 280.00m, Category = "Eletrônicos", StockQuantity = 15, ImageUrl = "/assets/products/headset-generico.png" }
        };

        var seededProductNames = seededProducts.Select(p => p.Name).ToArray();
        var existingProducts = db.Products
            .Where(p => seededProductNames.Contains(p.Name))
            .ToList();
        var existingProductNames = existingProducts.Select(p => p.Name).ToHashSet();
        var missingProducts = seededProducts.Where(p => !existingProductNames.Contains(p.Name)).ToArray();

        // Migra somente as imagens externas usadas anteriormente pelos produtos
        // iniciais. Imagens alteradas pelo administrador são preservadas.
        foreach (var existingProduct in existingProducts.Where(p =>
                     p.ImageUrl != null && p.ImageUrl.Contains("images.unsplash.com")))
        {
            existingProduct.ImageUrl = seededProducts
                .First(p => p.Name == existingProduct.Name)
                .ImageUrl;
        }

        if (missingProducts.Any()) db.Products.AddRange(missingProducts);
        db.SaveChanges();

        // O primeiro administrador só é criado com credenciais fornecidas pelo
        // ambiente. Nenhuma senha administrativa padrão fica no código.
        if (!db.AdministrativeAccounts.Any())
        {
            var bootstrapEmail = Environment.GetEnvironmentVariable("ADMIN_BOOTSTRAP_EMAIL");
            var bootstrapPassword = Environment.GetEnvironmentVariable("ADMIN_BOOTSTRAP_PASSWORD");
            var bootstrapName = Environment.GetEnvironmentVariable("ADMIN_BOOTSTRAP_FULL_NAME") ?? "Administrador inicial";

            if (!string.IsNullOrWhiteSpace(bootstrapEmail) && !string.IsNullOrWhiteSpace(bootstrapPassword))
            {
                db.AdministrativeAccounts.Add(new AdministrativeAccount
                {
                    Email = bootstrapEmail.Trim(),
                    PasswordHash = SecurityService.HashPassword(bootstrapPassword),
                    FullName = bootstrapName.Trim()
                });
                db.SaveChanges();
            }
            else
            {
                services.GetRequiredService<ILogger<Program>>().LogWarning(
                    "Nenhuma conta administrativa existe. Configure ADMIN_BOOTSTRAP_EMAIL e ADMIN_BOOTSTRAP_PASSWORD para criar a primeira conta.");
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

if (storageProvider.Equals("Local", StringComparison.OrdinalIgnoreCase))
{
    var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
    Directory.CreateDirectory(uploadsPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads"
    });
}

app.MapControllers();

app.MapGet("/uploads/{fileName}", async (
    string fileName,
    IStorageService storage,
    CancellationToken cancellationToken) =>
{
    var file = await storage.GetFileAsync(fileName, cancellationToken);
    return file is null
        ? Results.NotFound()
        : Results.Stream(file.Content, file.ContentType);
});

// Endpoints de Probes de Saúde
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();
