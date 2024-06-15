namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public class CharactersIdClient : BaseClient, ICharactersIdClient
{
    private string CharacterName { get; }

    internal CharactersIdClient(IGw2ApiClient apiClient, string characterName)
        : base(apiClient)
    {
        CharacterName = characterName;
        Backstory = new CharactersIdBackstoryClient(apiClient, characterName);
    }

    protected override string UriPath => $"/v2/characters/{CharacterName}";

    public ICharactersIdBackstoryClient Backstory { get; }
}
