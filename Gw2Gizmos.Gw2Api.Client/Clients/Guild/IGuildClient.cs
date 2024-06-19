namespace Gw2Gizmos.Gw2Api.Client.Clients.Guild;

public interface IGuildClient
{
    public IGuildIdClient this[string guildId] { get; }

    public IGuildPermissionsClient Permissions { get; }
    public IGuildSearchClient Search { get; }
    public IGuildUpgradesClient Upgrades { get; }
}
