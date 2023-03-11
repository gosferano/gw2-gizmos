namespace Gw2Gizmos.Gw2Api.Contract.Items;

public struct ItemGameType
{
    public static ItemGameType Activity = new(ItemGameTypes.Activity);
    public static ItemGameType Dungeon = new(ItemGameTypes.Dungeon);
    public static ItemGameType Pve = new(ItemGameTypes.Pve);
    public static ItemGameType Pvp = new(ItemGameTypes.Pvp);
    public static ItemGameType PvpLobby = new(ItemGameTypes.PvpLobby);
    public static ItemGameType Wvw = new(ItemGameTypes.Wvw);

    public string Value { get; }

    private ItemGameType(string value)
    {
        Value = value;
    }

    public static implicit operator ItemGameType(string value) => new(value);
}