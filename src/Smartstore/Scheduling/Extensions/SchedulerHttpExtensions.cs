using Microsoft.AspNetCore.Http;
using Smartstore.Scheduling;

namespace Smartstore
{
    public static class SchedulerHttpExtensions
    {
        /// <summary>
        /// Checks whether the current requests is called by the task scheduler.
        /// </summary>
        public static bool IsCalledByTaskScheduler(this HttpRequest request)
        {
            Guard.NotNull(request, nameof(request));
            return request.Headers.ContainsKey(DefaultTaskScheduler.AuthTokenName);
        }
    }
}
