namespace Gw2Gizmos.Desktop.Core.Navigation;

/// <summary>
/// Every page of the app, as a UI-agnostic key. View-models navigate and build breadcrumbs with these; each UI head
/// maps them onto its own page/view types.
/// </summary>
public enum AppPage
{
    Onboarding,
    Dashboard,
    Account,
    AccountValue,
    Wallet,
    MaterialStorage,
    Bank,
    SharedInventory,
    Characters,
    Character,
    CharacterInventory,
    Sessions,
    Session,
    SessionLoot,
    Events,
    WorldMap,
    Wvw,
    Items,
    TradingPost,
    PriceAlerts,
    Notifications,
    Logs,
    ApiKeys,
    Settings,
    AdvancedSettings,
    StoredData,
}
