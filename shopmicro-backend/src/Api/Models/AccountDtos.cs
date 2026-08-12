namespace Api.Models;

public sealed class AccountLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class MarketplaceAccountRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Phone { get; set; }
    public string? Cep { get; set; }
    public string? Address { get; set; }
}

public sealed class MarketplaceAccountUpdateRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Phone { get; set; }
    public string? Cep { get; set; }
    public string? Address { get; set; }
}

public sealed class AdministrativeAccountRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    public string? CurrentPassword { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}
