namespace Gw2Gizmos.Gw2Api.Contract.BuildStorage;

public class BuildStorageSpecialization
{
    public int Id { get; set; }
    public int?[] Traits { get; set; } = Array.Empty<int?>();
}
