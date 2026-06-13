using System;
using System.IO;
using System.Text.Json;
using Gw2Gizmos.Data.Worker.Updaters;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// File-backed <see cref="IDeliveryBaselineStore"/> for the desktop's in-process delivery poller. Keeps the
/// last-seen delivery box in a per-user JSON file so a delivery that arrived while the app was off is still
/// detected on the next run — without writing the worker-owned ingestion database.
/// </summary>
public sealed class FileDeliveryBaselineStore : IDeliveryBaselineStore
{
    private readonly string _path;
    private readonly object _gate = new();

    public FileDeliveryBaselineStore(AppPaths paths)
    {
        _path = paths.File("delivery-baseline.json");
    }

    public DeliveryBaseline? Load()
    {
        lock (_gate)
        {
            try
            {
                return File.Exists(_path)
                    ? JsonSerializer.Deserialize<DeliveryBaseline>(File.ReadAllText(_path))
                    : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public void Save(DeliveryBaseline baseline)
    {
        lock (_gate)
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(baseline));
        }
    }
}
