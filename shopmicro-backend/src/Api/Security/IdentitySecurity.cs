namespace Api.Security;

public static class IdentityScopes
{
    public const string ClaimName = "identity_scope";
    public const string Marketplace = "marketplace";
    public const string Administration = "administration";
}

public static class AuthorizationPolicies
{
    public const string MarketplaceOnly = "MarketplaceOnly";
    public const string AdministrationOnly = "AdministrationOnly";
}
