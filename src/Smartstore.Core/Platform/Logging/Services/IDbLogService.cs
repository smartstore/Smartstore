namespace Smartstore.Core.Logging;

/// <summary>
/// Manager for log entity database storage.
/// </summary>
public interface IDbLogService
{
    /// <summary>
    /// Truncates the log table completely and sets the auto-increment to 0.
    /// </summary>
    /// <param name="cancelToken">Cancellation token</param>
    /// <returns>Numer of deleted log entities.</returns>
    Task<int> ClearLogsAsync(CancellationToken cancelToken = default);

    /// <summary>
    /// Deletes log entities from database.
    /// </summary>
    /// <param name="maxAgeUtc">
    /// Max UTC date of log entities to delete (inclusive).
    /// </param>
    /// <param name="minLevelToRetain">
    /// The minimum log level for messages to be retained.
    /// </param>
    /// <returns>Numer of deleted log entities.</returns>
    Task<int> ClearLogsAsync(DateTime maxAgeUtc, LogLevel minLevelToRetain, CancellationToken cancelToken = default);
}
