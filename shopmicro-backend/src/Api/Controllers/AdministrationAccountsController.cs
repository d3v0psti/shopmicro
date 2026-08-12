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
[Route("api/administration")]
public sealed class AdministrationAccountsController : ControllerBase
{
    private const string RefreshCookie = "shopmicro_administration_refresh_token";
    private readonly AppDbContext _db;

    public AdministrationAccountsController(AppDbContext db) => _db = db;

    [AllowAnonymous]
    [HttpPost("auth/login")]
    public async Task<IActionResult> Login(AccountLoginRequest request)
    {
        var account = await _db.AdministrativeAccounts.FirstOrDefaultAsync(item => item.Email == request.Email);
        if (account is null || !SecurityService.VerifyPassword(request.Password, account.PasswordHash))
            return Unauthorized(new { message = "E-mail ou senha incorretos." });

        var token = CreateAccessToken(account);
        await CreateRefreshToken(account);
        return Ok(new
        {
            token,
            account.Email,
            account.FullName,
            identityScope = IdentityScopes.Administration
        });
    }

    [Authorize(Policy = AuthorizationPolicies.AdministrationOnly)]
    [HttpGet("accounts")]
    public async Task<IActionResult> ListAdministrativeAccounts() => Ok(await _db.AdministrativeAccounts
        .OrderBy(account => account.FullName)
        .Select(account => new { account.Email, account.FullName })
        .ToListAsync());

    [Authorize(Policy = AuthorizationPolicies.AdministrationOnly)]
    [HttpPost("accounts")]
    public async Task<IActionResult> CreateAdministrativeAccount(AdministrativeAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "E-mail e senha são obrigatórios." });
        if (await _db.AdministrativeAccounts.AnyAsync(account => account.Email == request.Email))
            return BadRequest(new { message = "E-mail administrativo já cadastrado." });

        _db.AdministrativeAccounts.Add(new AdministrativeAccount
        {
            Email = request.Email.Trim(),
            FullName = request.FullName.Trim(),
            PasswordHash = SecurityService.HashPassword(request.Password)
        });
        await _db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, new { message = "Conta administrativa criada com sucesso." });
    }

    [Authorize(Policy = AuthorizationPolicies.AdministrationOnly)]
    [HttpPut("accounts/{email}/password")]
    public async Task<IActionResult> ChangeAdministrativePassword(string email, ChangePasswordRequest request)
    {
        var account = await _db.AdministrativeAccounts.FirstOrDefaultAsync(item => item.Email == email);
        if (account is null) return NotFound(new { message = "Conta administrativa não encontrada." });
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Nova senha é obrigatória." });

        if (IsCurrentAccount(email))
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword)
                || !SecurityService.VerifyPassword(request.CurrentPassword, account.PasswordHash))
                return Unauthorized(new { message = "Senha atual incorreta." });
        }

        account.PasswordHash = SecurityService.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Senha atualizada com sucesso." });
    }

    [Authorize(Policy = AuthorizationPolicies.AdministrationOnly)]
    [HttpDelete("accounts/{email}")]
    public async Task<IActionResult> DeleteAdministrativeAccount(string email)
    {
        if (IsCurrentAccount(email))
            return BadRequest(new { message = "A conta em uso não pode ser excluída." });
        if (await _db.AdministrativeAccounts.CountAsync() <= 1)
            return BadRequest(new { message = "Não é possível excluir a única conta administrativa." });

        var account = await _db.AdministrativeAccounts.FirstOrDefaultAsync(item => item.Email == email);
        if (account is null) return NotFound(new { message = "Conta administrativa não encontrada." });
        _db.AdministrativeAccounts.Remove(account);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Conta administrativa excluída." });
    }

    [Authorize(Policy = AuthorizationPolicies.AdministrationOnly)]
    [HttpGet("marketplace-accounts")]
    public async Task<IActionResult> ListMarketplaceAccounts() => Ok(await _db.MarketplaceAccounts
        .OrderBy(account => account.FullName)
        .Select(account => new
        {
            account.Email,
            account.FullName,
            account.Cpf,
            account.Phone,
            account.Cep,
            account.Address
        })
        .ToListAsync());

    [Authorize(Policy = AuthorizationPolicies.AdministrationOnly)]
    [HttpPost("marketplace-accounts")]
    public async Task<IActionResult> CreateMarketplaceAccount(MarketplaceAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "E-mail e senha são obrigatórios." });
        if (await _db.MarketplaceAccounts.AnyAsync(account => account.Email == request.Email))
            return BadRequest(new { message = "E-mail já cadastrado no marketplace." });

        _db.MarketplaceAccounts.Add(new MarketplaceAccount
        {
            Email = request.Email.Trim(),
            FullName = request.FullName.Trim(),
            PasswordHash = SecurityService.HashPassword(request.Password),
            Cpf = request.Cpf,
            Phone = request.Phone,
            Cep = request.Cep,
            Address = request.Address
        });
        await _db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created, new { message = "Conta do marketplace criada." });
    }

    [Authorize(Policy = AuthorizationPolicies.AdministrationOnly)]
    [HttpPut("marketplace-accounts/{email}/password")]
    public async Task<IActionResult> ResetMarketplacePassword(string email, ChangePasswordRequest request)
    {
        var account = await _db.MarketplaceAccounts.FirstOrDefaultAsync(item => item.Email == email);
        if (account is null) return NotFound(new { message = "Conta do marketplace não encontrada." });
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Nova senha é obrigatória." });

        account.PasswordHash = SecurityService.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Senha atualizada com sucesso." });
    }

    [Authorize(Policy = AuthorizationPolicies.AdministrationOnly)]
    [HttpDelete("marketplace-accounts/{email}")]
    public async Task<IActionResult> DeleteMarketplaceAccount(string email)
    {
        var account = await _db.MarketplaceAccounts.FirstOrDefaultAsync(item => item.Email == email);
        if (account is null) return NotFound(new { message = "Conta do marketplace não encontrada." });

        var hasActiveOrders = await _db.Orders.AnyAsync(order =>
            order.CustomerEmail.ToLower() == email.ToLower() && order.Status != OrderStatus.Cancelled);
        if (hasActiveOrders)
            return BadRequest(new { message = "A conta possui pedidos ativos." });

        _db.MarketplaceAccounts.Remove(account);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Conta do marketplace excluída." });
    }

    [AllowAnonymous]
    [HttpPost("auth/refresh-token")]
    public async Task<IActionResult> RefreshAccessToken()
    {
        var record = await FindRefreshToken();
        if (record is null || record.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { message = "Sessão inválida ou expirada." });
        return Ok(new { token = CreateAccessToken(record.AdministrativeAccount) });
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

    private string CreateAccessToken(AdministrativeAccount account) => SecurityService.GenerateJwtToken(
        account.Email, account.FullName, "Admin", IdentityScopes.Administration, JwtSecret(), TimeSpan.FromHours(1));

    private async Task CreateRefreshToken(AdministrativeAccount account)
    {
        var value = SecurityService.GenerateRefreshToken();
        _db.AdministrativeRefreshTokens.Add(new AdministrativeRefreshToken
        {
            AdministrativeAccountId = account.Id,
            TokenHash = SecurityService.HashRefreshToken(value),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });
        await _db.SaveChangesAsync();
        Response.Cookies.Append(RefreshCookie, value, RefreshCookieOptions());
    }

    private async Task<AdministrativeRefreshToken?> FindRefreshToken()
    {
        if (!Request.Cookies.TryGetValue(RefreshCookie, out var value)) return null;
        var hash = SecurityService.HashRefreshToken(value);
        return await _db.AdministrativeRefreshTokens
            .Include(token => token.AdministrativeAccount)
            .FirstOrDefaultAsync(token => token.TokenHash == hash && !token.Revoked);
    }

    private bool IsCurrentAccount(string email) => string.Equals(CurrentEmail(), email, StringComparison.OrdinalIgnoreCase);
    private string? CurrentEmail() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    private static string JwtSecret() => SecurityService.GetRequiredJwtSecret();
    private static CookieOptions RefreshCookieOptions() => new()
    {
        HttpOnly = true,
        Secure = !string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase),
        SameSite = SameSiteMode.Strict,
        Expires = DateTimeOffset.UtcNow.AddDays(30),
        Path = "/api/administration"
    };
    private void DeleteRefreshCookie() => Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = "/api/administration" });
}
