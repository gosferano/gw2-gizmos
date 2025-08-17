using Gw2Gizmos.Data.EntityFramework;

namespace Gw2Gizmos.RecipeFinder.Cli;

public class ItemService : IItemService
{
    private readonly Gw2GizmosDbContext _dbContext;

    public ItemService(Gw2GizmosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GetItemNameAsync(int itemId, CancellationToken ct)
    {
        var item = await _dbContext.Items.FindAsync(new object[] { itemId }, ct);
        return item?.Name ?? "Unknown Item";
    }
}
