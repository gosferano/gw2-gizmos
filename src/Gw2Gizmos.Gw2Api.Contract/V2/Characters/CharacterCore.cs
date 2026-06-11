namespace Gw2Gizmos.Gw2Api.Contract.V2.Characters;

public class CharacterCore
{
    public string Name { get; set; } = null!;
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
}
