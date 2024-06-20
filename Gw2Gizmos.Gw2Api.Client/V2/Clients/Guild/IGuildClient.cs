namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Guild;

public interface IGuildClient
{
    public IGuildIdClient this[string guildId] { get; }

    public IGuildPermissionsClient Permissions { get; }
    public IGuildSearchClient Search { get; }
    public IGuildUpgradesClient Upgrades { get; }
}
