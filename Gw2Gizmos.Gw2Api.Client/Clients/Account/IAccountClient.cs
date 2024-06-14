namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public interface IAccountClient : IBlobClient<Contract.Account.Account>
{
    public AccountAchievementsClient Achievements { get; }
    public AccountBankClient Bank { get; }
}
