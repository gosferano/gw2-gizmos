namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Guild;

public sealed class GuildSearchNameClient : BaseClient, IGuildSearchNameClient
{
    private readonly string _guildName;

    internal GuildSearchNameClient(HttpClient httpClient, string guildName)
        : base(httpClient)
    {
        _guildName = guildName;
    }

    protected override string UriPath => $"/v2/guild/search";

    public Task<Result<string[], Error>> GetBlob(CancellationToken cancellationToken = default)
    {
        return Get<string[]>($"{UriPath}?name={_guildName}", SchemaVersion, cancellationToken);
    }
}
