namespace Gw2Gizmos.Gw2Api.Contract.V2.Skills;

public sealed class SkillFactComboFinisher : SkillFact
{
    public ComboFinisherType FinisherType { get; set; }
    public int Percent { get; set; }
}
