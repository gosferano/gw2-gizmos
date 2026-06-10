namespace Gw2Gizmos.Data.Worker.Notifications;

/// <summary>
/// MVP <see cref="INotifier"/> that writes the notification to the log. Replaced by a real toast
/// once the tray app exists.
/// </summary>
public sealed class LogNotifier : INotifier
{
    private readonly ILogger<LogNotifier> _logger;

    public LogNotifier(ILogger<LogNotifier> logger)
    {
        _logger = logger;
    }

    public void Notify(string title, string message)
    {
        _logger.LogInformation("[NOTIFY] {Title}: {Message}", title, message);
    }
}
