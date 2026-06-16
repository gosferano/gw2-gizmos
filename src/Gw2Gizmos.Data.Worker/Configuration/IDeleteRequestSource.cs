using System.Collections.Generic;

namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>
/// Supplies the pending data-delete requests the desktop has queued (see <see cref="DeleteRequest"/>). The worker
/// runs each request id it hasn't processed yet. Standalone workers have no desktop to queue them, so they use a
/// no-op source (see <see cref="NullDeleteRequestSource"/>).
/// </summary>
public interface IDeleteRequestSource
{
    IReadOnlyList<DeleteRequest> GetDeleteRequests();
}
