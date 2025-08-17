namespace Gw2Gizmos.RecipeFinder.Cli;

public interface IItemService
{
    Task<string> GetItemNameAsync(int itemId, CancellationToken ct);
}
