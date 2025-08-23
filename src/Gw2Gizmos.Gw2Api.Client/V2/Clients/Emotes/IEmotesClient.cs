using Gw2Gizmos.Gw2Api.Contract.V2.Emotes;

namespace Gw2Gizmos.Gw2Api.Client.V2.Clients.Emotes;

public interface IEmotesClient
    : IAllExpandableClient<Emote>,
        IBulkExpandableClient<Emote, string>,
        IPaginatedClient<Emote>;
