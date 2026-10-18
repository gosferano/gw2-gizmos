namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public sealed class WvwTimersClient : BaseBlobClient<string[]>, IWvwTimersClient
{
    internal WvwTimersClient(HttpClient httpClient)
        : base(httpClient)
    {
        Lockout = new WvwTimersLockoutClient(httpClient);
        TeamAssignment = new WvwTimersTeamAssignmentClient(httpClient);
    }

    protected override string UriPath => "/v2/wvw/timers";

    public IWvwTimersLockoutClient Lockout { get; }
    public IWvwTimersTeamAssignmentClient TeamAssignment { get; }
}
