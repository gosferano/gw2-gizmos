namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Recipes;

public interface IRecipesSearchClient
{
    public IRecipesSearchInputClient WithInputItemId(int inputItemId);
    public IRecipesSearchOutputClient WithOutputItemId(int outputItemId);
}
