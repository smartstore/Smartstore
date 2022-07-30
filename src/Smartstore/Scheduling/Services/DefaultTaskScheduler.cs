using System.Net;
using System.Net.Http;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Caching;
using Smartstore.Http;

namespace Smartstore.Scheduling
{
    internal class DefaultTaskScheduler : Disposable, ITaskScheduler
    {
        internal const string HttpClientName = "taskscheduler";
        internal const string RootPath = "taskscheduler";
        internal const string AuthTokenName = "X-SCHED-AUTH-TOKEN";

        private readonly ICacheManager _cache;
        private readonly IHttpClientFactory _httpClientFactory;

        private Timer _timer;
        private bool _shuttingDown;
        private int _errCount;

        public DefaultTaskScheduler(ICacheManager cache, IHttpClientFactory httpClientFactory)
        {
            _cache = cache;
            _httpClientFactory = httpClientFactory;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public int PollInterval { get; set; } = 1;

        public string BaseUrl { get; private set; }

        public bool IsActive => _timer != null;

        public string GetAsyncStateKey(int taskId)
        {
            return taskId.ToString();
        }

        public async Task<bool> VerifyAuthTokenAsync(string authToken)
        {
            if (authToken.IsEmpty())
                return false;

            var cacheKey = GenerateAuthTokenCacheKey(authToken);
            if (await _cache.ContainsAsync(cacheKey))
            {
                await _cache.RemoveAsync(cacheKey);
                return true;
            }

            return false;
        }

        public Task RunSingleTaskAsync(int taskId, IDictionary<string, string> taskParameters = null)
        {
            taskParameters ??= new Dictionary<string, string>();

            // User executes task in backend explicitly.
            taskParameters["explicit"] = "true";

            var qs = QueryString.Create(taskParameters);

            return CallEndpointAsync($"run/{taskId}{qs}", false);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Do nothing here, because it's too early to start off.
            // We gonna start in "Activate".
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _shuttingDown = true;
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Activate(string baseUrl, int pollInterval, HttpContext httpContext)
        {
            Guard.IsPositive(pollInterval, nameof(pollInterval));
            Guard.NotNull(httpContext, nameof(httpContext));

            var url = string.Empty;

            if (!httpContext.Connection.IsLocal() && baseUrl.HasValue())
            {
                if (!baseUrl.IsWebUrl())
                {
                    throw new ArgumentException("A valid base url is required for the web task scheduler.", nameof(baseUrl));
                }

                url = baseUrl.EnsureEndsWith('/') + RootPath + '/';
            }

            if (url.IsEmpty())
            {
                url = WebHelper.GetAbsoluteUrl(RootPath + '/', httpContext.Request);
            }

            BaseUrl = url;
            PollInterval = pollInterval;

            _timer?.Dispose();
            _timer = new Timer(DoPoll, "poll",
                TimeSpan.FromSeconds(GetFixedInterval(pollInterval)), // Next poll (must be whole minute)
                TimeSpan.FromMinutes(PollInterval)); // continous interval

            static double GetFixedInterval(int interval)
            {
                // Gets seconds to next poll minute
                int seconds = (interval * 60) - DateTime.Now.Second;
                return seconds;
            }
        }

        public async Task<HttpClient> CreateHttpClientAsync()
        {
            var baseUri = await WebHelper.CreateUriForSafeLocalCallAsync(new Uri(BaseUrl));

            var client = _httpClientFactory.CreateClient(HttpClientName);
            client.BaseAddress = baseUri;

            var authToken = await CreateAuthToken();
            client.DefaultRequestHeaders.Add(AuthTokenName, authToken);

            return client;
        }

        private async Task<string> CreateAuthToken()
        {
            string authToken = Guid.NewGuid().ToString();
            await _cache.PutAsync(GenerateAuthTokenCacheKey(authToken), true, new CacheEntryOptions().ExpiresIn(TimeSpan.FromMinutes(1)));

            return authToken;
        }

        private static string GenerateAuthTokenCacheKey(string authToken)
        {
            return "Scheduler:AuthToken:" + authToken;
        }

        private void DoPoll(object state)
        {
            _ = CallEndpointAsync((string)state, true);
        }

        private async Task CallEndpointAsync(string action, bool isPoll)
        {
            if (_shuttingDown || _errCount >= 10)
                return;

            // If passed uri is null, we always assume a poll action.
            action ??= "poll";
            var client = await CreateHttpClientAsync();

            try
            {
                using var response = await client.PostAsync(action, null);
                // Throw if not a success code.
                response.EnsureSuccessStatusCode();
                Interlocked.Exchange(ref _errCount, 0);
            }
            catch (Exception ex)
            {
                var uri = new Uri(client.BaseAddress, action);
                HandleException(ex, uri, out var isTimeout);

                if (isPoll || !isTimeout)
                {
                    Interlocked.Increment(ref _errCount);
                    if (_errCount >= 10)
                    {
                        // 10 failed attempts in succession. Stop the timer!
                        _timer?.Change(Timeout.Infinite, 0);
                        Logger.Info("Stopping TaskScheduler poll timer. Too many consecutive failed requests.");
                    }
                }
            }
        }

        private void HandleException(Exception exception, Uri uri, out bool isTimeout)
        {
            string msg = "Error while calling TaskScheduler endpoint '{0}'.".FormatInvariant(uri.OriginalString);
            var requestException = exception as HttpRequestException;

            isTimeout = (exception is TaskCanceledException tce && tce.InnerException is TimeoutException)
                || requestException?.StatusCode == HttpStatusCode.RequestTimeout
                || requestException?.StatusCode == HttpStatusCode.GatewayTimeout;

            if (requestException == null)
            {
                Logger.Error(exception, msg);
            }
            else
            {
                var statusCode = (int)requestException.StatusCode;
                if (statusCode < 500)
                {
                    // Any internal server error (>= 500) already handled by middleware
                    msg += " HTTP {0}, {1}".FormatCurrent(statusCode, requestException.StatusCode?.Humanize());
                    Logger.Error(msg);
                }
            }
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
                _timer?.Dispose();
        }
    }
}
