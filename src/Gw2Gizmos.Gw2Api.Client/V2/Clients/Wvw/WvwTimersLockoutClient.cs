using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public sealed class WvwTimersLockoutClient : BaseBlobClient<WvwTimer>, IWvwTimersLockoutClient
{
    internal WvwTimersLockoutClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/wvw/timers/lockout";
}
