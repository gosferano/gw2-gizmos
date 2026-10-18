using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public sealed class WvwTimersTeamAssignmentClient : BaseBlobClient<WvwTimer>, IWvwTimersTeamAssignmentClient
{
    internal WvwTimersTeamAssignmentClient(HttpClient httpClient)
        : base(httpClient) { }

    protected override string UriPath => "/v2/wvw/timers/teamAssignment";
}
