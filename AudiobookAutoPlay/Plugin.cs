using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AudiobookAutoPlay.Configuration;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Session;

namespace AudiobookAutoPlay;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private ISessionManager? _sessionManager;
    private ILibraryManager? _libraryManager;
    private bool _hooked;
    private readonly string _logPath;

    public override string Name => "Audiobook Auto-Play";
    public override Guid Id => Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
    public override string Description => "Automatically advances to the next chapter or audio file when an audiobook track finishes playing.";

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        _logPath = Path.Combine(applicationPaths.PluginConfigurationsPath, "AudiobookAutoPlay.log");
        Log("Plugin loaded");
    }

    public static Plugin? Instance { get; private set; }

    public void HookEvents(ISessionManager sessionManager, ILibraryManager libraryManager)
    {
        if (_hooked) return;
        _sessionManager = sessionManager;
        _libraryManager = libraryManager;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _hooked = true;
        Log("Playback listener active");
    }

    public void UnhookEvents()
    {
        if (!_hooked || _sessionManager == null) return;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        _hooked = false;
    }

    private async void OnPlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        try
        {
            await ProcessStop(e).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log("Error: " + ex.Message);
        }
    }

    private async Task ProcessStop(PlaybackStopEventArgs e)
    {
        var config = Configuration;
        if (config == null || !config.EnableAutoPlay)
            return;

        var session = e.Session;
        var item = e.Item;
        if (item == null || session == null)
            return;

        if (item.MediaType != MediaType.Audio)
            return;

        // Position-based completion detection.
        // Many clients report PlayedToCompletion=false even when the track
        // finishes naturally, so we also check if playback stopped within
        // the last 15 seconds of the track.
        long positionTicks = e.PlaybackPositionTicks ?? 0;
        long durationTicks = item.RunTimeTicks ?? 0;
        long remainingSeconds = 0;
        if (durationTicks > 0 && positionTicks > 0)
            remainingSeconds = (durationTicks - positionTicks) / TimeSpan.TicksPerSecond;

        bool nearEnd = (durationTicks > 0 && positionTicks > 0 && remainingSeconds < 15);
        bool finished = e.PlayedToCompletion || nearEnd;

        if (!finished)
            return;

        // Find next sibling audio item
        var parent = item.GetParent();
        if (parent == null || _libraryManager == null)
            return;

        var query = new InternalItemsQuery();
        query.Parent = parent;
        query.IncludeItemTypes = new[] { BaseItemKind.Audio, BaseItemKind.AudioBook };

        var items = _libraryManager.GetItemList(query);
        var siblings = new List<BaseItem>(items);
        siblings.Sort((a, b) => string.Compare(a.SortName, b.SortName, StringComparison.OrdinalIgnoreCase));

        BaseItem? nextItem = null;
        bool found = false;
        for (int i = 0; i < siblings.Count; i++)
        {
            if (found)
            {
                nextItem = siblings[i];
                break;
            }
            if (siblings[i].Id == item.Id)
                found = true;
        }

        if (nextItem == null)
            return;

        int delay = config.DelaySeconds;
        if (delay < 0) delay = 0;
        if (delay > 60) delay = 60;

        Log("Advancing: " + (item.Name ?? "?") + " -> " + (nextItem.Name ?? "?"));

        if (delay > 0)
            await Task.Delay(delay * 1000).ConfigureAwait(false);

        if (_sessionManager == null)
            return;

        try
        {
            var req = new PlayRequest();
            req.ItemIds = new[] { nextItem.Id };
            req.PlayCommand = PlayCommand.PlayNow;
            req.StartPositionTicks = 0;

            await _sessionManager.SendPlayCommand(
                session.Id,
                session.Id,
                req,
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log("SendPlayCommand failed: " + ex.Message);
        }
    }

    public void Log(string message)
    {
        try
        {
            File.AppendAllText(_logPath,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + Environment.NewLine);
        }
        catch { }
    }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "AudiobookAutoPlay",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
            }
        };
    }

    public void Dispose()
    {
        UnhookEvents();
    }
}
