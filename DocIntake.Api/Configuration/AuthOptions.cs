namespace DocIntake.Api.Configuration;

public class AuthOptions
{
    /// <summary>
    /// Name of the token claim that carries scopes, as a space-separated string or repeated claims.
    /// "scope" for dotnet user-jwts and Cognito; "scp" for Microsoft Entra ID.
    /// </summary>
    public string ScopeClaim { get; set; } = "scope";
}

/// <summary>Authorization policy names. They match the scope names in the token.</summary>
public static class AuthPolicies
{
    public const string Read = "documents.read";
    public const string Write = "documents.write";
}
