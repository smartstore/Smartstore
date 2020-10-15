using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Hosting;
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

            // TODO: (core) Enforce Run() methods to always create a new lifetime scope instead of calling BeginContextAwareScope (?)
        }

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
            // TODO: (core) Do what exactly? Ist this necessary?
        }

        #region Run methods

        public Task Run(
            Action<ILifetimeScope, CancellationToken> action,
            TaskCreationOptions options = TaskCreationOptions.LongRunning,
            TaskScheduler scheduler = null,
            CancellationToken cancellationToken = default)
        {
            Guard.NotNull(action, nameof(action));

            var ct = CreateCompositeCancellationToken(cancellationToken);

            var t = Task.Factory.StartNew(() =>
            {
                using (_scopeAccessor.BeginContextAwareScope())
                {
                    action(_scopeAccessor.LifetimeScope, ct);
                }
            }, ct, options, scheduler ?? TaskScheduler.Default);

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

            var ct = CreateCompositeCancellationToken(cancellationToken);

            var t = Task.Factory.StartNew((o) =>
            {
                using (_scopeAccessor.BeginContextAwareScope())
                {
                    action(_scopeAccessor.LifetimeScope, ct, o);
                }
            }, state, ct, options, scheduler ?? TaskScheduler.Default);

            return t;
        }

        public Task<TResult> Run<TResult>(
            Func<ILifetimeScope, CancellationToken, TResult> function,
            TaskCreationOptions options = TaskCreationOptions.LongRunning,
            TaskScheduler scheduler = null,
            CancellationToken cancellationToken = default)
        {
            Guard.NotNull(function, nameof(function));

            var ct = CreateCompositeCancellationToken(cancellationToken);

            var t = Task.Factory.StartNew(() =>
            {
                using (_scopeAccessor.BeginContextAwareScope())
                {
                    return function(_scopeAccessor.LifetimeScope, ct);
                }
            }, ct, options, scheduler ?? TaskScheduler.Default);

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

            var ct = CreateCompositeCancellationToken(cancellationToken);

            var t = Task.Factory.StartNew((o) =>
            {
                using (_scopeAccessor.BeginContextAwareScope())
                {
                    return function(_scopeAccessor.LifetimeScope, ct, o);
                }
            }, state, ct, options, scheduler ?? TaskScheduler.Default);

            return t;
        }

        public Task Run(
            Func<ILifetimeScope, CancellationToken, Task> function,
            object state,
            CancellationToken cancellationToken = default)
        {
            Guard.NotNull(state, nameof(state));
            Guard.NotNull(function, nameof(function));

            var ct = CreateCompositeCancellationToken(cancellationToken);
            var scope = _scopeAccessor.BeginLifetimeScope(null);

            Task task = null;

            try
            {
                task = function(scope, ct).ContinueWith(x =>
                {
                    scope.Dispose();
                }, ct);
            }
            catch
            {
                scope.Dispose();
            }

            return task;
        }

        #endregion
    }
}