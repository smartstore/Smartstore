using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Smartstore.Threading;

namespace Smartstore.Scheduling
{
    /// <summary>
    /// Responsible for polling the web schduler endpoint.
    /// </summary>
    public interface ITaskScheduler : IHostedService, IDisposable
    {
        /// <summary>
        /// The interval in minutes in which the scheduler triggers the polling url
        /// (which determines pending tasks and executes them in the scope of a regular HTTP request).
        /// </summary>
        int PollInterval { get; set; }

        /// <summary>
        /// The fully qualified base url. Without this being set to a valid url, the scheduler cannot poll for pending tasks.
        /// </summary>
        string BaseUrl { get; }

        /// <summary>
        ///  Gets a value indicating whether the scheduler is active and periodically polls all pending tasks.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Gets the unique key by which a <see cref="CancellationTokenSource"/> instance
        /// can be retrieved from an <see cref="IAsyncState"/> instance
        /// </summary>
        /// <param name="taskId">The task id</param>
        /// <returns>A unique string key</returns>
        string GetAsyncStateKey(int taskId);

        /// <summary>
        /// Verifies the authentication token which is generated right before the HTTP endpoint is called.
        /// </summary>
        /// <param name="authToken">The authentication token to verify</param>
        /// <returns><c>true</c> if the validation succeeds, <c>false</c> otherwise</returns>
        /// <remarks>
        /// The task scheduler sends the token as a HTTP request header item.
        /// The called endpoint (e.g. the <see cref="TaskSchedulerMiddleware"/>) is reponsible for invoking
        /// this method and quitting the tasks's execution - preferrably with HTTP 403 -
        /// if the verification fails.
        /// </remarks>
        Task<bool> VerifyAuthTokenAsync(string authToken);

        /// <summary>
        /// Creates a new <see cref="HttpClient"/> instance configured for the task scheduler.
        /// </summary>
        Task<HttpClient> CreateHttpClientAsync();

        /// <summary>
        /// Executes a single task immediately (without waiting for next schedule).
        /// </summary>
        /// <param name="taskId">Unique id of the task to run.</param>
        /// <param name="taskParameters">Optional task parameters.</param>
        /// <remarks>
        /// It's safe to call this method without waiting for completion (no await).
        /// </remarks>
        Task RunSingleTaskAsync(int taskId, IDictionary<string, string> taskParameters = null);

        /// <summary>
        /// Activates the scheduler. Must be called after <see cref="IHostedService.StartAsync(CancellationToken)"/>.
        /// Scheduler activation starts the polling.
        /// </summary>
        /// <param name="baseUrl">
        /// The base url including <see cref="HttpRequest.PathBase"/>, but without scheduler endpoint.
        /// If empty, <paramref name="httpContext"/> is used to auto-resolve the base url.
        /// </param>
        /// <param name="pollInterval">The scheduler endpoint poll interval in minutes.</param>
        void Activate(string baseUrl, int pollInterval, HttpContext httpContext);
    }
}
