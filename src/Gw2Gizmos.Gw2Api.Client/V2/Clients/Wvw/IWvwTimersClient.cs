namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

public interface IWvwTimersClient : IBlobClient<string[]>
{
    public IWvwTimersLockoutClient Lockout { get; }
    public IWvwTimersTeamAssignmentClient TeamAssignment { get; }
}
