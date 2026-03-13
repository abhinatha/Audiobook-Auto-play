using MediaBrowser.Model.Plugins;

namespace AudiobookAutoPlay.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Enable or disable auto-play globally.
    /// </summary>
    public bool EnableAutoPlay { get; set; } = true;

    /// <summary>
    /// Seconds to wait before starting the next chapter (0-60).
    /// </summary>
    public int DelaySeconds { get; set; } = 2;

    /// <summary>
    /// Show an on-screen countdown notification (reserved for future use).
    /// </summary>
    public bool ShowCountdownNotification { get; set; } = true;

    /// <summary>
    /// Stop at the end of the book rather than wrapping.
    /// </summary>
    public bool StopAtEndOfBook { get; set; } = true;

    /// <summary>
    /// Save the resume point before advancing (reserved for future use).
    /// </summary>
    public bool SavePositionBeforeAdvance { get; set; } = true;
}
