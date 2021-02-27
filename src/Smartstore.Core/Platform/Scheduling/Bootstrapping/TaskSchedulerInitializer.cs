using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Smartstore.Engine.Initialization;

namespace Smartstore.Core.Bootstrapping
{
    // TODO: (core) Implement TaskSchedulerInitializer
    internal class TaskSchedulerInitializer : IApplicationInitializer
    {
        public int Order => int.MinValue;
        public int MaxAttempts => 10;
        public bool ThrowOnError => false;

        public Task InitializeAsync(HttpContext httpContext)
        {
            return Task.CompletedTask;
        }

        public Task OnFailAsync(Exception exception, bool willRetry)
        {
            return Task.CompletedTask;
        }
    }
}
