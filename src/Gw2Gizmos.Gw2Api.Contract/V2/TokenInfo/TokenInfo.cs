namespace Gw2Gizmos.Gw2Api.Contract.V2.TokenInfo;

public class TokenInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TokenPermission[] Permissions { get; set; } = Array.Empty<TokenPermission>();
    public TokenType Type { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? IssuedAt { get; set; }
    public string[] Urls { get; set; } = Array.Empty<string>();
}
