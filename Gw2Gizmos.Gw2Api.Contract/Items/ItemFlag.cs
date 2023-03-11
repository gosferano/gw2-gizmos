namespace Gw2Gizmos.Gw2Api.Contract.Items;

public struct ItemFlag
{
    public static ItemFlag AccountBindOnUse = new(ItemFlags.AccountBindOnUse);
    public static ItemFlag AccountBound = new(ItemFlags.AccountBound);
    public static ItemFlag Attuned = new(ItemFlags.Attuned);
    public static ItemFlag BulkConsume = new(ItemFlags.BulkConsume);
    public static ItemFlag DeleteWarning = new(ItemFlags.DeleteWarning);
    public static ItemFlag HideSuffix = new(ItemFlags.HideSuffix);
    public static ItemFlag Infused = new(ItemFlags.Infused);
    public static ItemFlag MonsterOnly = new(ItemFlags.MonsterOnly);
    public static ItemFlag NoMysticForge = new(ItemFlags.NoMysticForge);
    public static ItemFlag NoSalvage = new(ItemFlags.NoSalvage);
    public static ItemFlag NoSell = new(ItemFlags.NoSell);
    public static ItemFlag NotUpgradeable = new(ItemFlags.NotUpgradeable);
    public static ItemFlag NoUnderwater = new(ItemFlags.NoUnderwater);
    public static ItemFlag SoulbindOnAcquire = new(ItemFlags.SoulbindOnAcquire);
    public static ItemFlag SoulBindOnUse = new(ItemFlags.SoulBindOnUse);
    public static ItemFlag Tonic = new(ItemFlags.Tonic);
    public static ItemFlag Unique = new(ItemFlags.Unique);

    public string Value { get; }

    private ItemFlag(string value)
    {
        Value = value;
    }

    public static implicit operator ItemFlag(string value) => new(value);
}