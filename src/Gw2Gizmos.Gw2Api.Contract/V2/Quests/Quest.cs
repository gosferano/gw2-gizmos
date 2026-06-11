namespace Gw2Gizmos.Gw2Api.Contract.V2.Quests;

public class Quest
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int Level { get; set; }
    public int Story { get; set; }
    public QuestGoal[] Goals { get; set; } = Array.Empty<QuestGoal>();
}
