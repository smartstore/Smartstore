using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine;

namespace Smartstore.Threading
{
    public partial class AsyncRunner
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ILifetimeScopeAccessor _scopeAccessor;

        public AsyncRunner(IHostApplicationLifetime appLifetime, ILifetimeScopeAccessor scopeAccessor)
        {
            _appLifetime = appLifetime;
            _scopeAccessor = scopeAccessor;

            _appLifetime.ApplicationStopping.Register(OnAppShutdown);
            AppShutdownCancellationToken = _appLifetime.ApplicationStopping;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        /// <summary>
        /// Gets the global cancellation token which signals the application shutdown
        /// </summary>
        public CancellationToken AppShutdownCancellationToken { get; }

        /// <summary>
        /// Combines the passed token with the global application shutdown token.
        /// </summary>
        public CancellationToken CreateCompositeCancellationToken(CancellationToken userCancellationToken)
        {
            return userCancellationToken == CancellationToken.None
                ? AppShutdownCancellationToken
                : CancellationTokenSource.CreateLinkedTokenSource(AppShutdownCancellationToken, userCancellationToken).Token;
        }

        private void OnAppShutdown()
        {
            // TODO: (core) Do what exactly? Is this necessary?
        }

        #region Run methods

        public Task Run(
            Action<ILifetimeScope, CancellationToken> action,
            TaskCreationOptions options = TaskCreationOptions.LongRunning,
            TaskScheduler scheduler = null,
            CancellationToken cancellationToken = default)
        {
            Guard.NotNull(action, nameof(action));

            var cancelToken = CreateCompositeCancellationToken(cancellationToken);
            var scope = _scopeAccessor.BeginLifetimeScope();

            var t = Task.Factory.StartNew(() => action(scope, cancelToken), 
                cancelToken, 
                options, 
                scheduler ?? TaskScheduler.Default);

            t.ContinueWith(t => TaskContinuation(t, scope), cancelToken);

            return t;
        }

        public Task Run(
            Action<ILifetimeScope, CancellationToken, object> action,
            object state,
            TaskCreationOptions options = TaskCreationOptions.LongRunning,
            TaskScheduler scheduler = null,
            CancellationToken cancellationToken = default)
        {
            Guard.NotNull(state, nameof(state));
            Guard.NotNull(action, nameof(action));

            var cancelToken = CreateCompositeCancellationToken(cancellationToken);
            var scope = _scopeAccessor.BeginLifetimeScope();

            var t = Task.Factory.StartNew((o) => action(scope, cancelToken, o), 
                state, 
                cancelToken,
                options, 
                scheduler ?? TaskScheduler.Default);

            t.ContinueWith(t => TaskContinuation(t, scope), cancelToken);

            return t;
        }

        public Task<TResult> Run<TResult>(
            Func<ILifetimeScope, CancellationToken, TResult> function,
            TaskCreationOptions options = TaskCreationOptions.LongRunning,
            TaskScheduler scheduler = null,
            CancellationToken cancellationToken = default)
        {
            Guard.NotNull(function, nameof(function));

            var cancelToken = CreateCompositeCancellationToken(cancellationToken);
            var scope = _scopeAccessor.BeginLifetimeScope();

            var t = Task.Factory.StartNew(() => function(scope, cancelToken), 
                cancelToken, 
                options, 
                scheduler ?? TaskScheduler.Default);

            t.ContinueWith(t => TaskContinuation(t, scope), cancelToken);

            return t;
        }

        public Task<TResult> Run<TResult>(
            Func<ILifetimeScope, CancellationToken, object, TResult> function,
            object state,
            TaskCreationOptions options = TaskCreationOptions.LongRunning,
            TaskScheduler scheduler = null,
            CancellationToken cancellationToken = default)
        {
            Guard.NotNull(state, nameof(state));
            Guard.NotNull(function, nameof(function));

            var cancelToken = CreateCompositeCancellationToken(cancellationToken);
            var scope = _scopeAccessor.BeginLifetimeScope();

            var t = Task.Factory.StartNew((o) => function(scope, cancelToken, o), 
                state, 
                cancelToken, 
                options, 
                scheduler ?? TaskScheduler.Default);

            t.ContinueWith(t => TaskContinuation(t, scope), cancelToken);

            return t;
        }

        public Task Run(
            Func<ILifetimeScope, CancellationToken, Task> function,
            object state,
            CancellationToken cancellationToken = default)
        {
            Guard.NotNull(state, nameof(state));
            Guard.NotNull(function, nameof(function));

            var cancelToken = CreateCompositeCancellationToken(cancellationToken);
            var scope = _scopeAccessor.BeginLifetimeScope();

            Task t = function(scope, cancelToken);
            t.ContinueWith(t => TaskContinuation(t, scope), cancelToken);

            return t;
        }

        private void TaskContinuation(Task task, ILifetimeScope scope)
        {
            scope.Dispose();

            if (task.Exception != null)
            {
                task.Exception.Flatten().InnerExceptions.Each(x => Logger.Error(x));
            }
        }

        #endregion
    }
}