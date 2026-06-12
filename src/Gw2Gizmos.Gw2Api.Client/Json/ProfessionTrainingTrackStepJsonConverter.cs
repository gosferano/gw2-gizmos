using Gw2Gizmos.Gw2Api.Contract.V2.Professions;

namespace Gw2Gizmos.Gw2Api.Client.Json;

public sealed class ProfessionTrainingTrackStepJsonConverter : PolymorphicJsonConverter<ProfessionTrainingTrackStep>
{
    protected override string TypePropertyName { get; } = "type";
    protected override Dictionary<string, Type> TypeMap { get; } =
        new()
        {
            { ProfessionTrainingTrackStepType.Skill, typeof(ProfessionTrainingTrackStepSkill) },
            { ProfessionTrainingTrackStepType.Trait, typeof(ProfessionTrainingTrackStepTrait) }
        };
}
