namespace Gw2Gizmos.Gw2Api.Contract.Quests;

public class Quest
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public int Story { get; set; }
    public QuestGoal[] Goals { get; set; } = Array.Empty<QuestGoal>();
}
