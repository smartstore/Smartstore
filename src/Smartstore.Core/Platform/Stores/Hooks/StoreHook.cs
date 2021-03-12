using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using Smartstore.Engine;
using Smartstore.Net;
using Smartstore.Scheduling;

namespace Smartstore.Core.Stores
{
    internal class StoreHook : AsyncDbSaveHook<Store>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        //private readonly IStoreContext _storeContext;
        //private readonly ITaskScheduler _taskScheduler;
        private readonly SmartConfiguration _appConfig;

        public StoreHook(
            IHttpContextAccessor httpContextAccessor,
            SmartConfiguration appConfig)
        {
            _httpContextAccessor = httpContextAccessor;
            _appConfig = appConfig;            
        }

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext != null && !_appConfig.TaskSchedulerBaseUrl.IsWebUrl())
            {
                // TODO: (core) Set missing task scheduler base URL in StoreHook.
                //_taskScheduler.SetBaseUrl(_storeService, _httpContext);

                //const string rootPath = "taskscheduler";
                //var url = string.Empty;

                //if (!httpContext.Connection.IsLocal())
                //{
                //    var defaultStore = _storeContext.GetAllStores().FirstOrDefault(x => x.IsStoreDataValid());
                //    if (defaultStore != null)
                //    {
                //        url = defaultStore.Url.EnsureEndsWith("/") + rootPath;
                //    }
                //}

                //if (url.IsEmpty() && httpContext.Request != null)
                //{
                //    url = WebHelper.GetAbsoluteUrl(rootPath + '/', httpContext.Request);
                //}

                //_taskScheduler.BaseUrl = url;
            }

            return Task.FromResult(HookResult.Ok);
        }
    }
}
