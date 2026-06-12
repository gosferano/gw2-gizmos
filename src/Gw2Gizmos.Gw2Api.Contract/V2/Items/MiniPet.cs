namespace Gw2Gizmos.Gw2Api.Contract.V2.Items;

public sealed class MiniPet : Item
{
    public MiniPetDetails Details { get; set; } = null!;
}

public sealed class MiniPetDetails
{
    public int MinipetId { get; set; }
}
