using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gw2Gizmos.Data.EntityFramework.Entities.Items;

[Table("MiniPets")]
public class MiniPet : Item
{
    // Navigation
    public MiniPetDetails Details { get; set; }
}

[Table("MiniPetDetails")]
public class MiniPetDetails
{
    [Key, ForeignKey(nameof(MiniPet))]
    public int ItemId { get; set; }
    public int MinipetId { get; set; }

    // Navigation
    public MiniPet MiniPet { get; set; }
}
