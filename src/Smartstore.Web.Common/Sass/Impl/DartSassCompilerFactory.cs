#nullable enable

using Smartstore.Core.Common.Configuration;
using Smartstore.Threading;

namespace Smartstore.Web.Sass;

/// <summary>
/// Creates the native Dart Sass compiler and applies the current keep-in-memory setting to each lease.
/// </summary>
internal sealed class DartSassCompilerFactory : ISassCompilerFactory, IAsyncDisposable
{
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(3);

    private readonly Work<PerformanceSettings> _settings;
    private readonly IdleResource<ISassCompiler> _compiler;

    /// <summary>
    /// Creates the Sass-specific facade over the shared resource lifetime manager.
    /// </summary>
    /// <param name="settings">Resolves the current performance setting at each compiler acquisition.</param>
    public DartSassCompilerFactory(Work<PerformanceSettings> settings)
    {
        _settings = Guard.NotNull(settings);

        // Keep the generic lifetime owner unaware of Sass; this factory supplies creation,
        // native-process disposal and logging for failures in its background idle task.
        _compiler = new IdleResource<ISassCompiler>(
            () => new DartSassCompiler(),
            compiler => ((DartSassCompiler)compiler).DisposeAsync(),
            IdleTimeout,
            ex => Logger.Error(ex, "Failed to stop the idle Dart Sass compiler process."));
    }

    public ILogger Logger { get; set; } = NullLogger.Instance;

    internal bool HasCompiler => _compiler.HasValue;

    public ValueTask<ResourceLease<ISassCompiler>> GetCompilerAsync(CancellationToken cancellationToken = default)
    {
        // The setting is resolved on acquisition, so an idle change needs no background polling.
        var keepInMemory = _settings.Value.KeepSassCompilerInMemory;
        return _compiler.AcquireAsync(keepInMemory, cancellationToken);
    }

    public ValueTask DisposeAsync() => _compiler.DisposeAsync();
}
