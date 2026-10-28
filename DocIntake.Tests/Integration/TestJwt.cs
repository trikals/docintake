using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DocIntake.Tests.Integration;

/// <summary>Creates signed tokens for tests. The key exists only in test code and signs nothing real.</summary>
public static class TestJwt
{
    public const string Issuer = "test-issuer";
    public const string Audience = "test-audience";

    public static readonly SymmetricSecurityKey Key =
        new(Encoding.UTF8.GetBytes("test-only-signing-key-0123456789-abcdef-0123456789"));

    public static string Create(string subject, params string[] scopes)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = subject,
                ["scope"] = string.Join(' ', scopes)
            }
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
