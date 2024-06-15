namespace Gw2Gizmos.Gw2Api.Client.Clients.Characters;

public interface ICharactersClient
{
    ICharactersIdClient this[string characterId] { get; }
}
