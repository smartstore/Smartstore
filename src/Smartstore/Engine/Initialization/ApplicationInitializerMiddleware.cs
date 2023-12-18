using Autofac;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smartstore.Diagnostics;
using Smartstore.Events;
using Smartstore.Threading;

namespace Smartstore.Engine.Initialization
{
    public sealed class ApplicationInitializedEvent
    {
        public HttpContext HttpContext { get; init; }
    }

    /// <summary>
    /// Middleware that executes <see cref="IApplicationInitializer"/> implementations during the very first request.
    /// </summary>
    /// <remarks>
    /// I really did NOT want this to be a middleware, but it turns out that we need host, port and application path
    /// during startup (for scheduler, PDF engine etc.). A middleware guarantees access to <see cref="HttpContext"/>
    /// which gives us all required data. I tried hard, but I really found no F*ING way to obtain this data before the
    /// first request (only when hosting model is "OutOfProcess", but we don't want that for perf reasons).
    /// So we have to live with the fact that this middleware stays active for ALL requests (though doing nothing after
    /// successfull initialization).
    /// We could have implemented initialization as a global MVC filter (which we can remove upon success), but filters jump in
    /// too late for us, so that was not an option.
    /// </remarks>
    public class ApplicationInitializerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IApplicationContext _appContext;
        private readonly AsyncRunner _asyncRunner;
        private readonly ILogger<ApplicationInitializerMiddleware> _logger;

        private bool _initialized;
        private List<InitModuleInfo> _initModuleInfos;

        public ApplicationInitializerMiddleware(
            RequestDelegate next,
            IApplicationContext appContext,
            AsyncRunner asyncRunner,
            ILogger<ApplicationInitializerMiddleware> logger)
        {
            _next = next;
            _appContext = appContext;
            _asyncRunner = asyncRunner;
            _logger = logger;
        }

        public static event EventHandler<EventArgs> Initialized;
        private static void RaiseInitialized(object sender)
        {
            Initialized?.Invoke(sender, new EventArgs());
        }

        public async Task Invoke(HttpContext context)
        {
            if (!_initialized)
            {
                var errorFeature = context.Features.Get<IExceptionHandlerPathFeature>();

                if (errorFeature == null)
                {
                    // Don't run initializers when re-execution after an exception is in progress.

                    var lockProvider = context.RequestServices.GetRequiredService<IDistributedLockProvider>();
                    var @lock = lockProvider.GetLock("ApplicationInitializerMiddleware.Initialize");

                    await using (await @lock.AcquireAsync(cancelToken: _asyncRunner.AppShutdownCancellationToken))
                    {
                        if (!_initialized)
                        {
                            await InitializeAsync(context);
                        }
                    }
                }
            }

            await _next(context);
        }

        private async Task InitializeAsync(HttpContext context)
        {
            var scope = context.GetServiceScope();
            var pendingModules = GetInitModuleInfos();

            var modules = pendingModules
                .Select(x => new InitModule
                {
                    Info = x,
                    Instance = scope.InjectProperties(scope.ResolveUnregistered(x.ModuleType) as IApplicationInitializer)
                })
                //.Where(x => x.Info.Attempts < Math.Max(1, x.Instance.MaxAttempts))
                .OrderBy(x => x.Instance.Order)
                .ToArray();

            if (modules.Length > 0)
            {
                _logger.Info($"Starting app initialization with {modules.Length} initializers.");
            }

            foreach (var module in modules)
            {
                var info = module.Info;
                var instance = module.Instance;
                var maxAttempts = Math.Max(1, instance.MaxAttempts);
                var fail = false;
                
                try
                {
                    _logger.Debug($"Executing initializer '{info.ModuleType.Name}'.");
                    info.Attempts++;

                    using (new AutoStopwatch($"App initializer: {info.ModuleType.Name}"))
                    {
                        await instance.InitializeAsync(context);
                    } 
                }
                catch (Exception ex)
                {
                    fail = true;

                    if (info.Attempts <= maxAttempts)
                    {
                        // Don't pollute event log 
                        _logger.Error(ex, "Error while executing application initializer '{0}': {1}", info.ModuleType, ex.Message);
                    }

                    if (instance.ThrowOnError)
                    {
                        throw;
                    }
                    else
                    {
                        await instance.OnFailAsync(ex, info.Attempts < maxAttempts);
                    }
                }
                finally
                {
                    var tooManyFailures = info.Attempts >= maxAttempts;
                    var canRemove = !fail || (!instance.ThrowOnError && tooManyFailures);

                    if (canRemove)
                    {
                        pendingModules.Remove(info);
                    }
                }
            }

            if (pendingModules.Count == 0)
            {
                // No pending initializers anymore.
                // Don't run this middleware from now on.
                _initialized = true;
                RaiseInitialized(this);

                var eventPublisher = _appContext.Services.Resolve<IEventPublisher>();
                await eventPublisher.PublishAsync(new ApplicationInitializedEvent { HttpContext = context });
            }
        }

        private List<InitModuleInfo> GetInitModuleInfos()
        {
            if (_initModuleInfos == null)
            {
                _initModuleInfos = _appContext.TypeScanner.FindTypes<IApplicationInitializer>()
                    .Select(x => new InitModuleInfo { ModuleType = x })
                    .ToList();
            }

            return _initModuleInfos;
        }

        class InitModuleInfo
        {
            public Type ModuleType { get; set; }
            public int Attempts { get; set; }
        }

        class InitModule
        {
            public InitModuleInfo Info { get; set; }
            public IApplicationInitializer Instance { get; set; }
        }
    }
}
