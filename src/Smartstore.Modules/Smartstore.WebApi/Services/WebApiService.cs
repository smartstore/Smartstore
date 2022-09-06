using System;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Caching;
using Smartstore.Engine.Modularity;
using Smartstore.WebApi.Models;

namespace Smartstore.WebApi.Services
{
    public partial class WebApiService : IWebApiService
    {
        internal const string StateKey = "smartstore.webapi:state";

        private readonly ICacheManager _cache;
        private readonly IServiceProvider _serviceProvider;

        public WebApiService(ICacheManager cache, IServiceProvider serviceProvider)
        {
            _cache = cache;
            _serviceProvider = serviceProvider;
        }

        public WebApiState GetState()
        {
            return _cache.Get(StateKey, (o) =>
            {
                o.ExpiresIn(TimeSpan.FromDays(30));

                var descriptor = _serviceProvider.GetService<IModuleCatalog>().GetModuleByName(Module.SystemName);
                var settings = _serviceProvider.GetService<WebApiSettings>();

                var state = new WebApiState
                {
                    IsActive = descriptor?.IsInstalled() ?? false,
                    ModuleVersion = descriptor?.Version?.ToString()?.NullEmpty() ?? "1.0",
                    LogUnauthorized = settings.LogUnauthorized,
                    MaxTop = settings.MaxTop,
                    MaxExpansionDepth = settings.MaxExpansionDepth
                };

                return state;
            });
        }

        public Task<WebApiState> GetStateAsync()
            => Task.FromResult(GetState());
    }
}
