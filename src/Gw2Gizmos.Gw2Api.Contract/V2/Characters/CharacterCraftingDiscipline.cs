namespace Gw2Gizmos.Gw2Api.Contract.V2.Characters;

public sealed class CharacterCraftingDiscipline
{
    public CraftingDisciplineName Discipline { get; set; }
    public int Rating { get; set; }
    public bool Active { get; set; }
}
