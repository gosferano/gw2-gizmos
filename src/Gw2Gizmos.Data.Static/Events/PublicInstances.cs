namespace Gw2Gizmos.Data.Static.Events;

/// <summary>
/// Hardcoded schedule (UTC) for the public-instance events: the Eye of the North rotation (Twisted
/// Marionette, Battle for Lion's Arch, Dragonstorm, Tower of Nightmares — every 2 hours) and the two
/// Convergences (Mount Balrior and Outer Nayos — every 3 hours). Times computed from the GW2 wiki
/// event-timer data; each carries its instance entry's chat link.
/// </summary>
public static class PublicInstances
{
    private static readonly TimeSpan TwoHours = TimeSpan.FromHours(2);
    private static readonly TimeSpan ThreeHours = TimeSpan.FromHours(3);

    private const string EyeOfTheNorth = "[&BAkMAAA=]";

    public static readonly IReadOnlyList<ScheduledEvent> All = new[]
    {
        // The Eye of the North rotation shipped with the Icebrood Saga.
        Instance("twisted-marionette", "Twisted Marionette", "Eye of the North", EyeOfTheNorth, Schedule.Every(TwoHours, T("00:00")), 20, Expansion.IcebroodSaga),
        Instance("battle-for-lions-arch", "Battle For Lion's Arch", "Eye of the North", EyeOfTheNorth, Schedule.Every(TwoHours, T("00:30")), 15, Expansion.IcebroodSaga),
        Instance("dragonstorm", "Dragonstorm", "Eye of the North", EyeOfTheNorth, Schedule.Every(TwoHours, T("01:00")), 20, Expansion.IcebroodSaga),
        Instance("tower-of-nightmares", "Tower of Nightmares", "Eye of the North", EyeOfTheNorth, Schedule.Every(TwoHours, T("01:30")), 15, Expansion.IcebroodSaga),

        Instance("convergence-mount-balrior", "Convergence: Mount Balrior", "Mount Balrior", "[&BK4OAAA=]", Schedule.Every(ThreeHours, T("00:00")), 15, Expansion.JanthirWilds),
        Instance("convergence-outer-nayos", "Convergence: Outer Nayos", "Outer Nayos", "[&BB8OAAA=]", Schedule.Every(ThreeHours, T("01:30")), 15, Expansion.SecretsOfTheObscure),
    };

    private static TimeSpan T(string time) => TimeSpan.Parse(time);

    private static ScheduledEvent Instance(
        string id,
        string name,
        string map,
        string chatLink,
        IReadOnlyList<TimeSpan> times,
        int durationMinutes,
        Expansion expansion
    ) =>
        new()
        {
            Id = id,
            Name = name,
            Map = map,
            Kind = EventKind.PublicInstance,
            Expansion = expansion,
            ChatLink = chatLink,
            Duration = TimeSpan.FromMinutes(durationMinutes),
            DailyTimesUtc = times
        };
}
