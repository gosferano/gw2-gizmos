using Gw2Gizmos.Data.EntityFramework;
using Gw2Gizmos.Data.EntityFramework.Entities.Professions;
using Gw2Gizmos.Data.Worker.Updaters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gw2Gizmos.Data.Worker.Tests;

/// <summary>The professions sync stores /v2/professions and /v2/specializations, updating rows it has seen before.</summary>
public sealed class ProfessionsUpdaterTests : IDisposable
{
    private readonly WorkerDbFixture _fixture = new();
    private readonly RoutingHttpStub _stub = new();

    public void Dispose() => _fixture.Dispose();

    private ProfessionsUpdater NewUpdater(Gw2GizmosDbContext db) =>
        new(db, _stub.BuildClientFactory(), NullLogger<ProfessionsUpdater>.Instance);

    private const string Guardian =
        """{ "id": "Guardian", "name": "Guardian", "icon": "https://x/g.png", "icon_big": "https://x/g-big.png" }""";

    private static string SpecializationJson(int id, string name, bool elite) =>
        $$"""
        {
          "id": {{id}}, "name": "{{name}}", "profession": "Guardian", "elite": {{(elite ? "true" : "false")}},
          "minor_traits": [], "major_traits": [], "icon": "https://x/{{id}}.png", "background": "https://x/bg.png"
          {{(elite ? $", \"profession_icon\": \"https://x/{id}-p.png\", \"profession_icon_big\": \"https://x/{id}-pb.png\"" : "")}}
        }
        """;

    private async Task SyncAsync()
    {
        using Gw2GizmosDbContext db = _fixture.NewContext();
        await NewUpdater(db).UpdateProfessions(CancellationToken.None);
    }

    [Fact]
    public async Task Stores_professions_and_specializations_with_their_icons()
    {
        _stub.SetJson("/v2/professions", $"[{Guardian}]");
        _stub.SetJson("/v2/specializations",
            $"[{SpecializationJson(16, "Radiance", elite: false)}, {SpecializationJson(62, "Firebrand", elite: true)}]");

        await SyncAsync();

        Gw2GizmosDbContext read = _fixture.NewContext();
        Profession guardian = await read.Professions.SingleAsync();
        Assert.Equal(("Guardian", "https://x/g-big.png"), (guardian.Name, guardian.IconBig));

        Specialization core = await read.Specializations.SingleAsync(s => s.Id == 16);
        Assert.False(core.Elite);
        Assert.Null(core.ProfessionIcon);

        Specialization firebrand = await read.Specializations.SingleAsync(s => s.Id == 62);
        Assert.Equal(("Firebrand", "Guardian", true), (firebrand.Name, firebrand.Profession, firebrand.Elite));
        Assert.Equal("https://x/62-pb.png", firebrand.ProfessionIconBig);
    }

    [Fact]
    public async Task A_later_sync_updates_existing_rows()
    {
        _stub.SetJson("/v2/professions", $"[{Guardian}]");
        _stub.SetJson("/v2/specializations", $"[{SpecializationJson(62, "Old Name", elite: true)}]");
        await SyncAsync();

        _stub.SetJson("/v2/specializations", $"[{SpecializationJson(62, "Firebrand", elite: true)}]");
        await SyncAsync();

        Gw2GizmosDbContext read = _fixture.NewContext();
        Assert.Equal("Firebrand", (await read.Specializations.SingleAsync()).Name);
        Assert.Equal(1, await read.Professions.CountAsync());
    }
}
