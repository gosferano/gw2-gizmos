namespace Gw2Gizmos.MumbleLink.Contract;

/// <summary>The avatar's position and orientation in map coordinates (metres), from MumbleLink's <c>fAvatar*</c> fields.</summary>
public sealed class AvatarPose
{
    public Vector3F Position { get; init; }
    public Vector3F Front { get; init; }
    public Vector3F Top { get; init; }
}
