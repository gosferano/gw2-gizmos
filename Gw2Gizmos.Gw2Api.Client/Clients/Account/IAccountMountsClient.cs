namespace Gw2Gizmos.Gw2Api.Client.Clients.Account;

public interface IAccountMountsClient : IBlobClient<string[]>
{
    IAccountMountsSkinsClient Skins { get; }
    IAccountMountsTypesClient Types { get; }
}
