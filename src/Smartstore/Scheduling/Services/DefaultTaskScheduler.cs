using System.Net;
using System.Net.Http;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Caching;
using Smartstore.Http;
using Smartstore.Net.Http;
using Smartstore.Threading;

namespace Smartstore.Scheduling
{
    internal class DefaultTaskScheduler : Disposable, ITaskScheduler
    {
        internal const string HttpClientName = "taskscheduler";
        internal const string RootPath = "taskscheduler";
        internal const string AuthTokenName = "X-SCHED-AUTH-TOKEN";

        private readonly ICacheManager _cache;
        private readonly CancellationTokenSource _stopping;

        private Task<Uri> _baseUriTask;
        private Uri _baseUri;
        private Timer _timer;
        private int _errCount;

        public DefaultTaskScheduler(ICacheManager cache)
        {
            _cache = cache;
            _stopping = new CancellationTokenSource();
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public int PollInterval { get; set; } = 1;

        public string BaseUrl { get; private set; }

        public bool IsActive => _timer != null;

        internal bool IsStopping => _stopping.IsCancellationRequested;

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
            try
            {
                _stopping.Cancel();
            }
            catch
            {
                // Ignore exceptions thrown as a result of a cancellation.
            }

            DisposeInternal();

            return Task.CompletedTask;
        }

        public Task ActivateAsync(string baseUrl, int pollInterval, HttpContext httpContext)
        {
            if (IsActive)
            {
                throw new InvalidOperationException("The task scheduler is already activated.");
            }
            
            Guard.IsPositive(pollInterval);
            Guard.NotNull(httpContext);

            var url = string.Empty;

            if (baseUrl.HasValue())
            {
                if (!baseUrl.IsWebUrl())
                {
                    throw new ArgumentException("A valid base url is required for the web task scheduler.", nameof(baseUrl));
                }

                url = baseUrl.EnsureEndsWith('/') + RootPath + '/';
            }

            if (url.IsEmpty())
            {
                url = WebHelper.GetAbsoluteUrl("~/" + RootPath + '/', httpContext.Request);
            }

            BaseUrl = url;
            PollInterval = pollInterval;

            // Don't await here, await later on first usage.
            _baseUriTask = WebHelper.CreateUriForSafeLocalCallAsync(new Uri(BaseUrl));

            _timer?.Dispose();
            _timer = NonCapturingTimer.Create(OnTimerTick, "poll",
                // Next poll (must be whole minute)
                TimeSpan.FromSeconds(GetFixedInterval(pollInterval)),
                // continous interval
                TimeSpan.FromMinutes(PollInterval));

            return Task.CompletedTask;

            static double GetFixedInterval(int interval)
            {
                // Gets seconds to next poll minute
                int seconds = (interval * 60) - DateTime.Now.Second;
                return seconds;
            }
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

        // Yes, async void. We need to be async. We need to be void. We handle the exceptions in CallEndpointAsync.
        private async void OnTimerTick(object state)
        {
            await CallEndpointAsync((string)state, true);
        }

        private async Task CallEndpointAsync(string action, bool isPoll)
        {
            if (IsStopping || _errCount >= 10)
            {
                return;
            }

            // If passed uri is null, we always assume a poll action.
            action ??= "poll";

            try
            {
                // Forcibly yield - we want to unblock the timer thread.
                await Task.Yield();

                using var client = await CreateHttpClientAsync();
                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, action);

                // Auth token header
                requestMessage.Headers.Add(AuthTokenName, await CreateAuthToken());


                // Call endpoint
                using var response = await client.SendAsync(requestMessage, _stopping.Token);
                
                // Throw if not a success code.
                response.EnsureSuccessStatusCode();

                // Success: reset error count.
                Interlocked.Exchange(ref _errCount, 0);
            }
            catch (OperationCanceledException) when (IsStopping)
            {
                // This is a cancellation - if the app is shutting down we want to ignore it.
                // Otherwise, it's a timeout and we want to handle it.
            }
            catch (Exception ex)
            {
                var uri = new Uri(_baseUri, action);
                HandleException(ex, uri, out var isTimeout);

                if (isPoll || !isTimeout)
                {
                    Interlocked.Increment(ref _errCount);
                    if (_errCount >= 10)
                    {
                        // 10 failed attempts in succession. Stop the timer!
                        _timer?.Change(Timeout.Infinite, 0);
                        Logger.Warn("Stopping TaskScheduler poll timer. Too many consecutive failed requests.");
                    }
                }
            }
        }

        public async Task<HttpClient> CreateHttpClientAsync()
        {
            if (_baseUriTask == null)
            {
                if (_baseUri == null)
                {
                    throw new InvalidOperationException("The task scheduler is not in activated state.");
                }
            }
            else
            {
                _baseUri = _baseUriTask.IsCompleted ? _baseUriTask.Result : await _baseUriTask;
                _baseUriTask = null;
            }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
            };

            // Don't obtain HttpClient from factory: app shutdown will leak otherwise.
            var client = new HttpClient(handler, true)
            {
                // INFO: avoids HttpClient.Timeout error messages in the log list.
                // Only affects the HTTP request that starts the task. Does not affect the execution of the task.
                Timeout = TimeSpan.FromMinutes(240),
                BaseAddress = _baseUri,
            };

            // User agent header
            client.DefaultRequestHeaders.UserAgent.Add(HttpClientBuilderExtensions.UserAgentHeader);

            return client;
        }

        private void HandleException(Exception exception, Uri uri, out bool isTimeout)
        {
            string msg = "Error while calling TaskScheduler endpoint '{0}'.".FormatInvariant(uri.OriginalString);
            var requestException = exception as HttpRequestException;
            var statusCode = requestException?.StatusCode;

            isTimeout = (exception is TaskCanceledException tce && tce.InnerException is TimeoutException)
                || statusCode == HttpStatusCode.RequestTimeout
                || statusCode == HttpStatusCode.GatewayTimeout;

            if (statusCode == null)
            {
                Logger.Error(exception, msg);
            }
            else if ((int)statusCode < 500)
            {
                // Any internal server error (>= 500) already handled by middleware
                msg += " HTTP {0}, {1}".FormatInvariant((int)statusCode, statusCode.Humanize());
                Logger.Error(msg);
            }
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                DisposeInternal();
            }  
        }

        private void DisposeInternal()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
