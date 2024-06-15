namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public interface IAccountClient : IBlobClient<Contract.Account.Account>
{
    public IAccountAchievementsClient Achievements { get; }
    public IAccountBankClient Bank { get; }
}
