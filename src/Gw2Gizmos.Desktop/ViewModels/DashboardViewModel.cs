using System.Linq;
using Gw2Gizmos.Data.EntityFramework;
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
    }

    public string ApiKeyStatus { get; }

    public int ItemCount { get; }

    public int NotificationCount { get; }
}
