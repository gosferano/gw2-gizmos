namespace Gw2Gizmos.Data.Worker.Updaters;

/// <summary>The last-seen trading-post delivery box, used to detect new deliveries across restarts.</summary>
public sealed record DeliveryBaseline(int Coins, Dictionary<int, int> Items);

/// <summary>
/// Persists the delivery baseline for <see cref="CommerceDeliveryUpdater"/>. The engine stays storage-agnostic:
/// the host supplies the backing store (the desktop writes a per-user file; a default keeps it in memory).
/// </summary>
public interface IDeliveryBaselineStore
{
    /// <summary>The persisted baseline, or <c>null</c> if none has been saved yet.</summary>
    DeliveryBaseline? Load();

    void Save(DeliveryBaseline baseline);
}

/// <summary>In-memory default so the engine works without a host-supplied store (baseline resets on restart).</summary>
public sealed class InMemoryDeliveryBaselineStore : IDeliveryBaselineStore
{
    private DeliveryBaseline? _baseline;

    public DeliveryBaseline? Load() => _baseline;

    public void Save(DeliveryBaseline baseline) => _baseline = baseline;
}
