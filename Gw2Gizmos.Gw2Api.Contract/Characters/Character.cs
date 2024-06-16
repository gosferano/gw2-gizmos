namespace Gw2Gizmos.Gw2Api.Contract.Characters;

public class Character
{
    public string Name { get; set; }
    public RaceName Race { get; set; }
    public Gender Gender { get; set; }
    public ProfessionName Profession { get; set; }
    public int Level { get; set; }
    public string? Guild { get; set; }
    public int Age { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public DateTimeOffset Created { get; set; }
    public int Deaths { get; set; }
    public int? Title { get; set; }
    public CharacterCraftingDiscipline[] Crafting { get; set; } = Array.Empty<CharacterCraftingDiscipline>();
    public string[] Backstory { get; set; } = Array.Empty<string>();
    public CharacterWvwAbility[] WvwAbilities { get; set; } = Array.Empty<CharacterWvwAbility>();
    public int BuildTabsUnlocked { get; set; }
    public int ActiveBuildTab { get; set; }
    public CharacterBuildTab[] BuildTabs { get; set; } = Array.Empty<CharacterBuildTab>();
    public int EquipmentTabsUnlocked { get; set; }
    public int ActiveEquipmentTab { get; set; }
    public CharacterEquipmentItem[] Equipment { get; set; } = Array.Empty<CharacterEquipmentItem>();
    public CharacterEquipmentTab[] EquipmentTabs { get; set; } = Array.Empty<CharacterEquipmentTab>();
    public int[] Recipes { get; set; } = Array.Empty<int>();
    public CharacterTrainingItem[] Training { get; set; } = Array.Empty<CharacterTrainingItem>();
    public CharacterInventoryBag[] Bags { get; set; } = Array.Empty<CharacterInventoryBag>();
}
