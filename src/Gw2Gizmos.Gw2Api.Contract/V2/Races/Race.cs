namespace Gw2Gizmos.Gw2Api.Contract.V2.Races;

public class Race
{
    public string Id { get; set; }
    public int[] Skills { get; set; } = Array.Empty<int>();
}
