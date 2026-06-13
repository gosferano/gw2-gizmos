using System.Collections.Generic;
using System.Linq;
using Gw2Gizmos.Desktop.Mvvm;

namespace Gw2Gizmos.Desktop;

/// <summary>
/// Backs the Notifications settings page: one global on/off toggle per notification category. Turning a
/// category off suppresses its toasts app-wide — independent of the per-event reminder opt-ins, which stay
/// on the Events screen.
/// </summary>
public sealed class NotificationsViewModel : ViewModelBase
{
    public NotificationsViewModel(NotificationSettingsStore settings)
    {
        Categories = NotificationCategories.All
            .Select(info => new NotificationCategoryToggle(info, settings))
            .ToList();
    }

    public IReadOnlyList<NotificationCategoryToggle> Categories { get; }
}

/// <summary>One category's global enable toggle; writes through to the settings store on change.</summary>
public sealed class NotificationCategoryToggle : ViewModelBase
{
    private readonly NotificationSettingsStore _settings;
    private readonly string _key;
    private bool _isEnabled;

    public NotificationCategoryToggle(NotificationCategoryInfo info, NotificationSettingsStore settings)
    {
        _key = info.Key;
        Name = info.Name;
        Description = info.Description;
        _settings = settings;
        _isEnabled = settings.IsEnabled(info.Key);
    }

    public string Name { get; }

    public string Description { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                _settings.SetEnabled(_key, value);
            }
        }
    }
}
