namespace Gw2Gizmos.MumbleLink.Client.Marshalling;

/// <summary>
/// Composes a decoded <see cref="LinkedMem"/> (plus the parsed identity/context) into the public
/// <see cref="MumbleLinkSnapshot"/>. A pure function — the seam where all three parsers come together.
/// </summary>
internal static class SnapshotMapper
{
    public static MumbleLinkSnapshot Compose(LinkedMem mem)
    {
        return new MumbleLinkSnapshot
        {
            UiVersion = mem.UiVersion,
            UiTick = mem.UiTick,
            Avatar = new AvatarPose
            {
                Position = mem.AvatarPosition,
                Front = mem.AvatarFront,
                Top = mem.AvatarTop,
            },
            Camera = new CameraPose
            {
                Position = mem.CameraPosition,
                Front = mem.CameraFront,
                Top = mem.CameraTop,
            },
            Name = mem.Name,
            Identity = IdentityParser.Parse(mem.Identity),
            Context = mem.ContextLen > 0 ? ContextParser.Parse(mem.Context) : null,
            Description = mem.Description,
        };
    }
}
