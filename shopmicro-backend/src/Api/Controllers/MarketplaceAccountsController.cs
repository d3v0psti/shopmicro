using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Api.Data;
using Api.Models;
using Api.Security;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/marketplace")]
public sealed class MarketplaceAccountsController : ControllerBase
{
    private const string RefreshCookie = "shopmicro_marketplace_refresh_token";
    private readonly AppDbContext _db;

    public MarketplaceAccountsController(AppDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpPost("accounts")]
    public async Task<IActionResult> Register(MarketplaceAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "E-mail e senha são obrigatórios." });

        if (await _db.MarketplaceAccounts.AnyAsync(account => account.Email == request.Email))
            return BadRequest(new { message = "E-mail já cadastrado no marketplace." });

        _db.MarketplaceAccounts.Add(new MarketplaceAccount
        {
            Email = request.Email.Trim(),
            PasswordHash = SecurityService.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Cpf = request.Cpf,
            Phone = request.Phone,
            Cep = request.Cep,
            Address = request.Address
        });
        await _db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, new { message = "Conta do marketplace criada com sucesso." });
    }

    [AllowAnonymous]
    [HttpPost("auth/login")]
    public async Task<IActionResult> Login(AccountLoginRequest request)
    {
        var account = await _db.MarketplaceAccounts.FirstOrDefaultAsync(item => item.Email == request.Email);
        if (account is null || !SecurityService.VerifyPassword(request.Password, account.PasswordHash))
            return Unauthorized(new { message = "E-mail ou senha incorretos." });

        var token = CreateAccessToken(account);
        await CreateRefreshToken(account);
        return Ok(new
        {
            token,
            account.Email,
            account.FullName,
            account.Cpf,
            account.Phone,
            account.Cep,
            account.Address,
            identityScope = IdentityScopes.Marketplace
        });
    }

    [Authorize(Policy = AuthorizationPolicies.MarketplaceOnly)]
    [HttpPut("accounts/{email}")]
    public async Task<IActionResult> Update(string email, MarketplaceAccountUpdateRequest request)
    {
        if (!OwnsAccount(email)) return Forbid();

        var account = await _db.MarketplaceAccounts.FirstOrDefaultAsync(item => item.Email == email);
        if (account is null) return NotFound(new { message = "Conta não encontrada." });

        if (!string.Equals(email, request.Email, StringComparison.OrdinalIgnoreCase)
            && await _db.MarketplaceAccounts.AnyAsync(item => item.Email == request.Email))
            return BadRequest(new { message = "O novo e-mail já está cadastrado." });

        account.Email = request.Email.Trim();
        account.FullName = request.FullName.Trim();
        account.Cpf = request.Cpf;
        account.Phone = request.Phone;
        account.Cep = request.Cep;
        account.Address = request.Address;
        await _db.SaveChangesAsync();
        return Ok(new
        {
            message = "Perfil atualizado com sucesso.",
            token = CreateAccessToken(account)
        });
    }

    [Authorize(Policy = AuthorizationPolicies.MarketplaceOnly)]
    [HttpDelete("accounts/{email}")]
    public async Task<IActionResult> Delete(string email)
    {
        if (!OwnsAccount(email)) return Forbid();

        var account = await _db.MarketplaceAccounts.FirstOrDefaultAsync(item => item.Email == email);
        if (account is null) return NotFound(new { message = "Conta não encontrada." });

        var hasActiveOrders = await _db.Orders.AnyAsync(order =>
            order.CustomerEmail.ToLower() == email.ToLower() && order.Status != OrderStatus.Cancelled);
        if (hasActiveOrders)
            return BadRequest(new { message = "Cancele os pedidos ativos antes de excluir a conta." });

        _db.MarketplaceAccounts.Remove(account);
        await _db.SaveChangesAsync();
        DeleteRefreshCookie();
        return Ok(new { message = "Conta excluída com sucesso." });
    }

    [AllowAnonymous]
    [HttpPost("auth/refresh-token")]
    public async Task<IActionResult> RefreshAccessToken()
    {
        var record = await FindRefreshToken();
        if (record is null || record.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { message = "Sessão inválida ou expirada." });

        return Ok(new { token = CreateAccessToken(record.MarketplaceAccount) });
    }

    [AllowAnonymous]
    [HttpPost("auth/logout")]
    public async Task<IActionResult> Logout()
    {
        var record = await FindRefreshToken();
        if (record is not null)
        {
            record.Revoked = true;
            await _db.SaveChangesAsync();
        }
        DeleteRefreshCookie();
        return Ok(new { message = "Logout realizado com sucesso." });
    }

    private string CreateAccessToken(MarketplaceAccount account) => SecurityService.GenerateJwtToken(
        account.Email, account.FullName, "Client", IdentityScopes.Marketplace, JwtSecret(), TimeSpan.FromHours(1));

    private async Task CreateRefreshToken(MarketplaceAccount account)
    {
        var value = SecurityService.GenerateRefreshToken();
        _db.MarketplaceRefreshTokens.Add(new MarketplaceRefreshToken
        {
            MarketplaceAccountId = account.Id,
            TokenHash = SecurityService.HashRefreshToken(value),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });
        await _db.SaveChangesAsync();
        Response.Cookies.Append(RefreshCookie, value, RefreshCookieOptions());
    }

    private async Task<MarketplaceRefreshToken?> FindRefreshToken()
    {
        if (!Request.Cookies.TryGetValue(RefreshCookie, out var value)) return null;
        var hash = SecurityService.HashRefreshToken(value);
        return await _db.MarketplaceRefreshTokens
            .Include(token => token.MarketplaceAccount)
            .FirstOrDefaultAsync(token => token.TokenHash == hash && !token.Revoked);
    }

    private bool OwnsAccount(string email) => string.Equals(CurrentEmail(), email, StringComparison.OrdinalIgnoreCase);
    private string? CurrentEmail() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    private static string JwtSecret() => SecurityService.GetRequiredJwtSecret();
    private static CookieOptions RefreshCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = !string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase),
        SameSite = SameSiteMode.Strict,
        Expires = DateTimeOffset.UtcNow.AddDays(30),
        Path = "/api/marketplace"
    };
    private void DeleteRefreshCookie() => Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/api/marketplace" });
}
