using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Currencies;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;

namespace Gw2Gizmos.Data.Worker.Updaters;

public class CurrenciesUpdater
{
    private readonly Gw2GizmosDbContext _dbContext;
    private readonly Gw2ApiClient _apiClient;
    private readonly ILogger<CommerceUpdater> _logger;

    public CurrenciesUpdater(
        Gw2GizmosDbContext dbContext,
        IGw2ApiClientFactory apiClientFactory,
        ILogger<CommerceUpdater> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _apiClient = apiClientFactory.Create(Locale.English);
    }

    public async Task UpdateCurrencies(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting currencies update...");

        Gw2Api.Contract.V2.Currencies.Currency[] currencies = await _apiClient.V2.Currencies.GetAll(stoppingToken);
        var existingCurrencies = await _dbContext.Currencies.ToListAsync(stoppingToken);

        foreach (var currency in currencies)
        {
            var existingCurrency = existingCurrencies.FirstOrDefault(c => c.Id == currency.Id);
            if (existingCurrency != null)
            {
                existingCurrency.Name = currency.Name;
                existingCurrency.Description = currency.Description;
                existingCurrency.Icon = currency.Icon;
                existingCurrency.Order = currency.Order;
            }
            else
            {
                _dbContext.Currencies.Add(
                    new Currency
                    {
                        Id = currency.Id,
                        Name = currency.Name,
                        Description = currency.Description,
                        Icon = currency.Icon,
                        Order = currency.Order
                    }
                );
            }
        }

        await _dbContext.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("Currencies update completed.");
    }
}
