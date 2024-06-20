using Gw2Gizmos.Gw2Api.Contract.Characters;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Characters;

public class CharactersBuildTabsClient : BaseBulkAllClient<CharacterBuildTab, int>, ICharactersBuildTabsClient
{
    private readonly string _characterId;

    internal CharactersBuildTabsClient(HttpClient httpClient, string characterId)
        : base(httpClient, "tabs")
    {
        _characterId = characterId;
        Active = new CharactersBuildTabsActiveClient(httpClient, characterId);
    }

    protected override string UriPath => $"/v2/characters/{_characterId}/buildtabs";

    public ICharactersBuildTabsActiveClient Active { get; }
}
