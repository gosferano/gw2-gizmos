namespace Gw2Gizmos.Gw2Api.Contract.V2.Wvw;

public class WvwMatchTeamValues<T>
    where T : notnull
{
    public T Red { get; set; } = default!;
    public T Blue { get; set; } = default!;
    public T Green { get; set; } = default!;
}
