namespace Gw2Gizmos.BuildMachine.Model.Skills;

public struct Attunement
{
    public static Attunement Fire = new("Fire");
    public static Attunement Water = new("Water");
    public static Attunement Air = new("Air");
    public static Attunement Earth = new("Earth");

    public Attunement(string value)
    {
        Value = value;
    }
    
    public string Value { get; }
}