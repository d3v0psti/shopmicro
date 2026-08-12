namespace Api.Models;

public sealed class MarketplaceRefreshToken
{
    public int Id { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public int MarketplaceAccountId { get; set; }
    public MarketplaceAccount MarketplaceAccount { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AdministrativeRefreshToken
{
    public int Id { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public int AdministrativeAccountId { get; set; }
    public AdministrativeAccount AdministrativeAccount { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
