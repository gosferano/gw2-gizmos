using Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Wvw;

/// <summary>
/// When the next team assignment locks in, after which changing WvW guild no longer affects it.
/// </summary>
public interface IWvwTimersLockoutClient : IBlobClient<WvwTimer>;
