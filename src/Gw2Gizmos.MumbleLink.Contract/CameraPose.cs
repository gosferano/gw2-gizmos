namespace Gw2Gizmos.MumbleLink.Contract;

/// <summary>The camera's position and orientation in map coordinates (metres), from MumbleLink's <c>fCamera*</c> fields.</summary>
public sealed class CameraPose
{
    public Vector3F Position { get; init; }
    public Vector3F Front { get; init; }
    public Vector3F Top { get; init; }
}
