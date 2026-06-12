namespace Gw2Gizmos.Gw2Api.Contract.V2.Masteries;

public sealed class MasteryLevel
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Instruction { get; set; } = null!;
    public string Icon { get; set; } = null!;
    public int PointCost { get; set; }
    public int ExpCost { get; set; }
}
