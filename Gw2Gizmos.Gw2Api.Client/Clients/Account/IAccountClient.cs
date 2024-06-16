namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public interface IAccountClient : IBlobClient<Contract.Account.Account>
{
    public IAccountAchievementsClient Achievements { get; }
    public IAccountBankClient Bank { get; }
    public IAccountBuildStorageClient BuildStorage { get; }
    public IAccountDailyCraftingClient DailyCrafting { get; }
    public IAccountDungeonsClient Dungeons { get; }
    public IAccountDyesClient Dyes { get; }
    public IAccountEmotesClient Emotes { get; }
    public IAccountFinishersClient Finishers { get; }
    public IAccountGlidersClient Gliders { get; }
    public IAccountHomeClient Home { get; }
    public IAccountInventoryClient Inventory { get; }
    public IAccountMaterialsClient Materials { get; }
}
