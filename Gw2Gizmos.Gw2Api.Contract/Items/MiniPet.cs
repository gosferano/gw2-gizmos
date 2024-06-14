namespace Gw2Gizmos.Gw2Api.Contract.Items;

public class MiniPet : Item
{
    public MiniPetDetails Details { get; set; }
}

public class MiniPetDetails
{
    public int MinipetId { get; set; }
}
