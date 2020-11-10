using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;

namespace Smartstore.Engine.Initialization
{
    internal class RootApplicationInitializer
    {
        private readonly IComponentContext _scope;
        private readonly ITypeScanner _typeScanner;
        private readonly ILogger<RootApplicationInitializer> _logger;

        public RootApplicationInitializer(
            IComponentContext scope,
            ITypeScanner typeScanner, 
            ILogger<RootApplicationInitializer> logger)
        {
            _scope = scope;
            _typeScanner = typeScanner;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            var instances = GetInitializers();
            if (instances.Length == 0)
            {
                _logger.Debug("Exiting app initialization. No initializer found.");
                return;
            }

            _logger.Info($"Starting app initialization with {instances.Length} initializers.");

            int numFailed = 0;

            foreach (var instance in instances)
            {
                try
                {
                    _logger.Debug($"Executing initializer '{instance.GetType().Name}'.");
                    await instance.InitializeAsync();
                }
                catch (Exception ex)
                {
                    if (instance.ThrowOnError)
                    {
                        _logger.Error(ex, "Error while executing application initializer '{0}': {1}", instance.GetType(), ex.Message);
                        throw;
                    }
                    else
                    {
                        numFailed++;
                        instance.OnFail(ex);
                    }
                }
            }

            var message = $"App initialization completed with '{instances.Length - numFailed}' initializers. Failed: {numFailed}.";
            if (numFailed > 0)
            {
                _logger.Warn(message);
            }
            else
            {
                _logger.Info(message);
            }
        }

        private IApplicationInitializer[] GetInitializers()
        {
            try
            {
                var initializerTypes = _typeScanner.FindTypes<IApplicationInitializer>(ignoreInactiveModules: true);

                return initializerTypes
                    .Select(x => _scope.ResolveUnregistered(x) as IApplicationInitializer)
                    .OrderBy(x => x.Order)
                    .ToArray();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while resolving application initializers.");
                throw;
            }

        }
    }
}