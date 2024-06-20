namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Emblem;

public interface IEmblemClient : IBlobClient<string[]>
{
    IEmblemBackgroundsClient Backgrounds { get; }
    IEmblemForegroundsClient Foregrounds { get; }
}
