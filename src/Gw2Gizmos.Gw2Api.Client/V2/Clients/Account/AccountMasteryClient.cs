namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Account;

public sealed class AccountMasteryClient : BaseClient, IAccountMasteryClient
{
    internal AccountMasteryClient(HttpClient httpClient)
        : base(httpClient)
    {
        Points = new AccountMasteryPointsClient(httpClient);
    }

    protected override string UriPath => "/v2/account/mastery";

    public IAccountMasteryPointsClient Points { get; }
}
