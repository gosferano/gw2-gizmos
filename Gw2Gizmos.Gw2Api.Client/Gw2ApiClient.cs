using Gw2Gizmos.Gw2Api.Client.Clients.Account;
using Gw2Gizmos.Gw2Api.Client.Clients.Achievements;
using Gw2Gizmos.Gw2Api.Client.Clients.Backstory;
using Gw2Gizmos.Gw2Api.Client.Clients.Build;
using Gw2Gizmos.Gw2Api.Client.Clients.Characters;
using Gw2Gizmos.Gw2Api.Client.Clients.Colors;
using Gw2Gizmos.Gw2Api.Client.Clients.Commerce;
using Gw2Gizmos.Gw2Api.Client.Clients.Continents;
using Gw2Gizmos.Gw2Api.Client.Clients.Currencies;
using Gw2Gizmos.Gw2Api.Client.Clients.DailyCrafting;
using Gw2Gizmos.Gw2Api.Client.Clients.Dungeons;
using Gw2Gizmos.Gw2Api.Client.Clients.Emblem;
using Gw2Gizmos.Gw2Api.Client.Clients.Emotes;
using Gw2Gizmos.Gw2Api.Client.Clients.Files;
using Gw2Gizmos.Gw2Api.Client.Clients.Finishers;
using Gw2Gizmos.Gw2Api.Client.Clients.Gliders;
using Gw2Gizmos.Gw2Api.Client.Clients.Guild;
using Gw2Gizmos.Gw2Api.Client.Clients.Home;
using Gw2Gizmos.Gw2Api.Client.Clients.Items;
using Gw2Gizmos.Gw2Api.Client.Clients.ItemStats;
using Gw2Gizmos.Gw2Api.Client.Clients.JadeBots;
using Gw2Gizmos.Gw2Api.Client.Clients.LegendaryArmory;
using Gw2Gizmos.Gw2Api.Client.Clients.Materials;
using Gw2Gizmos.Gw2Api.Client.Clients.Races;
using Gw2Gizmos.Gw2Api.Client.Clients.Specializations;

namespace Gw2Gizmos.Gw2Api.Client;

public class Gw2ApiClient
{
    private const string BaseUrl = "https://api.guildwars2.com";
    private const string AuthorizationHeaderName = "Authorization";
    private const string AcceptLanguageHeaderName = "Accept-Language";
    private const string UserAgentHeaderName = "User-Agent";

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
    public IMaterialsClient Materials { get; }
    public IRacesClient Races { get; }
    public ISpecializationsClient Specializations { get; }

    public Gw2ApiClient(HttpClient httpClient)
    {
        // Initialize clients
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
        Materials = new MaterialsClient(httpClient);
        Races = new RacesClient(httpClient);
        Specializations = new SpecializationsClient(httpClient);
    }

    public Gw2ApiClient()
        : this(null, null) { }

    public Gw2ApiClient(Locale locale)
        : this(null, locale) { }

    public Gw2ApiClient(string? accessToken, Locale? locale = null)
        : this(BuildDefaultHttpClient(accessToken, locale)) { }

    private static HttpClient BuildDefaultHttpClient(string? accessToken, Locale? locale)
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl), };

        httpClient.DefaultRequestHeaders.Add(UserAgentHeaderName, "Gw2Gizmos");

        if (accessToken != null)
        {
            httpClient.DefaultRequestHeaders.Add(AuthorizationHeaderName, $"Bearer {accessToken}");
        }

        if (locale != null)
        {
            httpClient.DefaultRequestHeaders.Add(AcceptLanguageHeaderName, locale);
        }

        return httpClient;
    }
}
