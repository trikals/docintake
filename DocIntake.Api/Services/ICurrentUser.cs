namespace DocIntake.Api.Services;

/// <summary>The signed-in user for the current request.</summary>
public interface ICurrentUser
{
    /// <summary>The token's "sub" claim: stable per user within an identity provider. Stored as OwnerId.</summary>
    string Id { get; }
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string Id => accessor.HttpContext?.User.FindFirst("sub")?.Value
        ?? throw new InvalidOperationException("There is no authenticated user on this request.");
}
