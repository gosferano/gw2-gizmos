using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Professions;
using Gw2Gizmos.Gw2Api.Client;
using Microsoft.EntityFrameworkCore;
using ApiProfessions = Gw2Gizmos.Gw2Api.Contract.V2.Professions;
using ApiSpecializations = Gw2Gizmos.Gw2Api.Contract.V2.Specializations;

namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>
/// Full-refresh ingester for professions (<c>/v2/professions</c>) and their specializations
/// (<c>/v2/specializations</c>), both public: a handful of rows each, changing only with game releases, so it runs
/// daily. Gives the character cards profession and elite specialization names and icons. Rows the API stops listing
/// are kept.
/// </summary>
public class ProfessionsUpdater
{
    private readonly Gw2GizmosDbContext _dbContext;
    private readonly Gw2ApiClient _apiClient;
    private readonly ILogger<ProfessionsUpdater> _logger;

    public ProfessionsUpdater(
        Gw2GizmosDbContext dbContext, IGw2ApiClientFactory apiClientFactory, ILogger<ProfessionsUpdater> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _apiClient = apiClientFactory.Create(Locale.English);
    }

    public async Task UpdateProfessions(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting professions update...");

        ApiProfessions.Profession[]? professions = await _apiClient.V2.Professions.GetAll(stoppingToken);
        ApiSpecializations.Specialization[]? specializations =
            await _apiClient.V2.Specializations.GetAll(stoppingToken);
        if (professions is null || professions.Length == 0 || specializations is null || specializations.Length == 0)
        {
            _logger.LogWarning("Professions API returned no data; skipping professions update.");
            return;
        }

        Dictionary<string, Profession> existingProfessions =
            await _dbContext.Professions.ToDictionaryAsync(p => p.Id, stoppingToken);
        foreach (ApiProfessions.Profession profession in professions)
        {
            if (!existingProfessions.TryGetValue(profession.Id, out Profession? row))
            {
                row = new Profession { Id = profession.Id };
                _dbContext.Professions.Add(row);
            }

            row.Name = profession.Name;
            row.Icon = profession.Icon;
            row.IconBig = profession.IconBig;
        }

        Dictionary<int, Specialization> existingSpecializations =
            await _dbContext.Specializations.ToDictionaryAsync(s => s.Id, stoppingToken);
        foreach (ApiSpecializations.Specialization specialization in specializations)
        {
            if (!existingSpecializations.TryGetValue(specialization.Id, out Specialization? row))
            {
                row = new Specialization { Id = specialization.Id };
                _dbContext.Specializations.Add(row);
            }

            row.Name = specialization.Name;
            row.Profession = specialization.Profession;
            row.Elite = specialization.Elite;
            row.Icon = specialization.Icon;
            row.ProfessionIcon = specialization.ProfessionIcon;
            row.ProfessionIconBig = specialization.ProfessionIconBig;
        }

        await _dbContext.SaveChangesAsync(stoppingToken);
        _logger.LogInformation(
            "Professions update completed: {Professions} professions, {Specializations} specializations.",
            professions.Length, specializations.Length);
    }
}
