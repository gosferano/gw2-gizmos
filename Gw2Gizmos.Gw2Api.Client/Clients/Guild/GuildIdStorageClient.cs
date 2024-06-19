using Gw2Gizmos.Gw2Api.Contract.Guild;

namespace Gw2Gizmos.Gw2Api.Client.Clients.Guild;

public class GuildIdStorageClient : BaseBlobClient<GuildStorageItem[]>, IGuildIdStorageClient
{
    private readonly string _guildId;

    internal GuildIdStorageClient(HttpClient httpClient, string guildId)
        : base(httpClient)
    {
        _guildId = guildId;
    }

    protected override string UriPath => $"/v2/guild/{_guildId}/storage";
}
