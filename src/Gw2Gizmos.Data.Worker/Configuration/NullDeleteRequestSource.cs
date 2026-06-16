using System;
using System.Collections.Generic;

namespace Gw2Gizmos.Data.Worker.Configuration;

/// <summary>The <see cref="IDeleteRequestSource"/> for a standalone worker: no desktop, so never any requests.</summary>
public sealed class NullDeleteRequestSource : IDeleteRequestSource
{
    public IReadOnlyList<DeleteRequest> GetDeleteRequests() => Array.Empty<DeleteRequest>();
}
