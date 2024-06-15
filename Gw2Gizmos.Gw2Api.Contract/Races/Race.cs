namespace Gw2Gizmos.Gw2Api.Contract.Races;

public class Race
{
    public string Id { get; set; }
    public int[] Skills { get; set; } = Array.Empty<int>();
}
