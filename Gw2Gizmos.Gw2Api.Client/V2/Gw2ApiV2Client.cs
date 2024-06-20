using Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Achievements;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Backstory;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Build;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Characters;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Colors;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Commerce;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Continents;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Currencies;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.DailyCrafting;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Dungeons;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Emblem;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Emotes;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Files;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Finishers;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Gliders;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Guild;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Home;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Items;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.ItemStats;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.JadeBots;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.LegendaryArmory;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Legends;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.MailCarriers;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.MapChests;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Maps;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Masteries;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Materials;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Minis;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Mounts;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Novelties;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Outfits;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Pets;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Professions;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Races;
using Gw2Gizmos.Gw2Api.Client.V2.Clients.Specializations;

namespace Gw2Gizmos.Gw2Api.Client.V2;

public class Gw2ApiV2Client : IGw2ApiV2Client
{
    internal Gw2ApiV2Client(HttpClient httpClient)
    {
        Account = new AccountClient(httpClient);
        Achievements = new AchievementsClient(httpClient);
        Backstory = new BackstoryClient(httpClient);
        Build = new BuildClient(httpClient);
        Characters = new CharactersClient(httpClient);
        Colors = new ColorsClient(httpClient);
        Commerce = new CommerceClient(httpClient);
        Continents = new ContinentsClient(httpClient);
        Currencies = new CurrenciesClient(httpClient);
        DailyCrafting = new DailyCraftingClient(httpClient);
        Dungeons = new DungeonsClient(httpClient);
        Emblem = new EmblemClient(httpClient);
        Emotes = new EmotesClient(httpClient);
        Files = new FilesClient(httpClient);
        Finishers = new FinishersClient(httpClient);
        Gliders = new GlidersClient(httpClient);
        Guild = new GuildClient(httpClient);
        Home = new HomeClient(httpClient);
        Items = new ItemsClient(httpClient);
        ItemStats = new ItemStatsClient(httpClient);
        JadeBots = new JadeBotsClient(httpClient);
        LegendaryArmory = new LegendaryArmoryClient(httpClient);
        Legends = new LegendsClient(httpClient);
        MailCarriers = new MailCarriersClient(httpClient);
        MapChests = new MapChestsClient(httpClient);
        Maps = new MapsClient(httpClient);
        Masteries = new MasteriesClient(httpClient);
        Materials = new MaterialsClient(httpClient);
        Minis = new MinisClient(httpClient);
        Mounts = new MountsClient(httpClient);
        Novelties = new NoveltiesClient(httpClient);
        Outfits = new OutfitsClient(httpClient);
        Pets = new PetsClient(httpClient);
        Professions = new ProfessionsClient(httpClient);
        Races = new RacesClient(httpClient);
        Specializations = new SpecializationsClient(httpClient);
    }

    public IAccountClient Account { get; }
    public IAchievementsClient Achievements { get; }
    public IBackstoryClient Backstory { get; }
    public IBuildClient Build { get; }
    public ICharactersClient Characters { get; }
    public IColorsClient Colors { get; }
    public ICommerceClient Commerce { get; }
    public IContinentsClient Continents { get; }
    public ICurrenciesClient Currencies { get; }
    public IDailyCraftingClient DailyCrafting { get; }
    public IDungeonsClient Dungeons { get; }
    public IEmblemClient Emblem { get; }
    public IEmotesClient Emotes { get; }
    public IFilesClient Files { get; }
    public IFinishersClient Finishers { get; }
    public IGlidersClient Gliders { get; }
    public IGuildClient Guild { get; }
    public IHomeClient Home { get; }
    public IItemsClient Items { get; }
    public IItemStatsClient ItemStats { get; }
    public IJadeBotsClient JadeBots { get; }
    public ILegendaryArmoryClient LegendaryArmory { get; }
    public ILegendsClient Legends { get; }
    public IMailCarriersClient MailCarriers { get; }
    public IMapChestsClient MapChests { get; }
    public IMapsClient Maps { get; }
    public IMasteriesClient Masteries { get; }
    public IMaterialsClient Materials { get; }
    public IMinisClient Minis { get; }
    public IMountsClient Mounts { get; }
    public INoveltiesClient Novelties { get; }
    public IOutfitsClient Outfits { get; }
    public IPetsClient Pets { get; }
    public IProfessionsClient Professions { get; }
    public IRacesClient Races { get; }
    public ISpecializationsClient Specializations { get; }
}
