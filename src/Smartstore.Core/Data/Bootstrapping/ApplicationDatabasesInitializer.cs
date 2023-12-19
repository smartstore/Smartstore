using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Smartstore.Core.Data.Migrations;
using Smartstore.Engine.Initialization;

namespace Smartstore.Core.Bootstrapping
{
    internal class ApplicationDatabasesInitializer : IApplicationInitializer
    {
        private readonly IDatabaseInitializer _initializer;
        private readonly IHostApplicationLifetime _appLifetime;

        public ApplicationDatabasesInitializer(IDatabaseInitializer initializer, IHostApplicationLifetime appLifetime)
        {
            _initializer = initializer;
            _appLifetime = appLifetime;
        }

        // Must be the ABSOLUTE FIRST initializer to run!
        public int Order => int.MinValue;
        public bool ThrowOnError => false;
        public int MaxAttempts => 1;

        public Task InitializeAsync(HttpContext httpContext)
            => _initializer.RunPendingSeedersAsync(_appLifetime.ApplicationStopping);

        public Task OnFailAsync(Exception exception, bool willRetry)
            => Task.CompletedTask;
    }
}
