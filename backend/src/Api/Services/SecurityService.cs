using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Api.Services;

public static class SecurityService
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 120_000;

    public static string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(KeySize);

        return $"$pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public static bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            var parts = hashedPassword.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4 || parts[0] != "pbkdf2-sha256")
                return false;

            var iterations = int.Parse(parts[1]);
            var salt = Convert.FromBase64String(parts[2]);
            var key = Convert.FromBase64String(parts[3]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(key.Length);
            return CryptographicOperations.FixedTimeEquals(computed, key);
        }
        catch
        {
            return false;
        }
    }

    public static byte[] GetSigningKeyBytes(string secret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        if (secretBytes.Length < 33)
        {
            using var sha512 = SHA512.Create();
            secretBytes = sha512.ComputeHash(secretBytes);
        }

        return secretBytes;
    }

    public static string GenerateJwtToken(string email, string fullName, string secret, TimeSpan expiresIn)
    {
        var key = new SymmetricSecurityKey(GetSigningKeyBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim("fullName", fullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.Add(expiresIn),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string HashRefreshToken(string refreshToken)
    {
        using var sha256 = SHA256.Create();
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = sha256.ComputeHash(tokenBytes);
        return Convert.ToBase64String(hashBytes);
    }

    public static bool VerifyRefreshToken(string refreshToken, string hashedToken)
    {
        var computedHash = HashRefreshToken(refreshToken);
        var computedBytes = Encoding.UTF8.GetBytes(computedHash);
        var hashedBytes = Encoding.UTF8.GetBytes(hashedToken);
        return CryptographicOperations.FixedTimeEquals(computedBytes, hashedBytes);
    }

    public static string GenerateRefreshToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(tokenBytes);
    }
}
