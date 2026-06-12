using System.Linq;
using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Recipes;
using Gw2Gizmos.Data.Worker.Configuration;
using Gw2Gizmos.Desktop.Mvvm;
using Microsoft.Extensions.DependencyInjection;

namespace Gw2Gizmos.Desktop;

public sealed class DashboardViewModel : ViewModelBase
{
    public DashboardViewModel(IServiceScopeFactory scopeFactory, AppStateApiKeyStore apiKeyStore)
    {
        ApiKeyStatus = apiKeyStore.HasApiKey ? "Configured" : "Not set";

        using IServiceScope scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Gw2GizmosDbContext>();
        ItemCount = dbContext.Items.Count();
        NotificationCount = dbContext.Notifications.Count();

        ProfitableRecipeCount = dbContext.ProfitableRecipes.Count();
        ProfitableRecipe? best = dbContext.ProfitableRecipes.OrderByDescending(r => r.Profit).FirstOrDefault();
        BestMargin = best is null ? "—" : $"best {best.MarginPercent:F0}% margin";
    }

    public string ApiKeyStatus { get; }

    public int ItemCount { get; }

    public int NotificationCount { get; }

    public int ProfitableRecipeCount { get; }

    public string BestMargin { get; }
}
