namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public class WvwMatchTeamValues<T>
    where T : notnull
{
    public T Red { get; set; }
    public T Blue { get; set; }
    public T Green { get; set; }
}
