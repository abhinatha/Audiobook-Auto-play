using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;

namespace AudiobookAutoPlay;

public class PlaybackListener : IHostedService
{
    private readonly ISessionManager _sessionManager;
    private readonly ILibraryManager _libraryManager;

    public PlaybackListener(ISessionManager sessionManager, ILibraryManager libraryManager)
    {
        _sessionManager = sessionManager;
        _libraryManager = libraryManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var plugin = Plugin.Instance;
        if (plugin != null)
        {
            plugin.HookEvents(_sessionManager, _libraryManager);
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        var plugin = Plugin.Instance;
        if (plugin != null)
        {
            plugin.UnhookEvents();
        }
        return Task.CompletedTask;
    }
}
