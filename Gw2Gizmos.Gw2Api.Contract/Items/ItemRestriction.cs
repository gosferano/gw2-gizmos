namespace Gw2Gizmos.Gw2Api.Contract.Items;

public struct ItemRestriction
{
    public static ItemRestriction Asura = new(ItemRestrictions.Asura);
    public static ItemRestriction Charr = new(ItemRestrictions.Charr);
    public static ItemRestriction Human = new(ItemRestrictions.Human);
    public static ItemRestriction Norn = new(ItemRestrictions.Norn);
    public static ItemRestriction Sylvari = new(ItemRestrictions.Sylvari);
    public static ItemRestriction Elementalist = new(ItemRestrictions.Elementalist);
    public static ItemRestriction Engineer = new(ItemRestrictions.Engineer);
    public static ItemRestriction Guardian = new(ItemRestrictions.Guardian);
    public static ItemRestriction Mesmer = new(ItemRestrictions.Mesmer);
    public static ItemRestriction Necromancer = new(ItemRestrictions.Necromancer);
    public static ItemRestriction Ranger = new(ItemRestrictions.Ranger);
    public static ItemRestriction Revenant = new(ItemRestrictions.Revenant);
    public static ItemRestriction Thief = new(ItemRestrictions.Thief);
    public static ItemRestriction Warrior = new(ItemRestrictions.Warrior);
    public static ItemRestriction Female = new(ItemRestrictions.Female);

    public string Value { get; }

    private ItemRestriction(string value)
    {
        Value = value;
    }

    public static implicit operator ItemRestriction(string value) => new(value);
}