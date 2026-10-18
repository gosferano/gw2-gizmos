namespace Gw2Gizmos.Desktop.Core.Ui;

/// <summary>Opens a folder in the system's file manager; implemented by each UI head.</summary>
public interface IFolderLauncher
{
    /// <summary>Opens <paramref name="path"/> in the file manager; false when it couldn't (e.g. none is set up).</summary>
    Task<bool> OpenAsync(string path);
}
