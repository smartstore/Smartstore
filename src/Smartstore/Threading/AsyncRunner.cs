using Autofac;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Engine;

namespace Smartstore.Threading
{
    /// <summary>
    /// Provides methods that run tasks in isolated dependency lifetime scopes,
    /// OR runs async methods synchronously.
    /// </summary>
    public partial class AsyncRunner
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly ILifetimeScopeAccessor _scopeAccessor;

        public AsyncRunner(IHostApplicationLifetime appLifetime, ILifetimeScopeAccessor scopeAccessor)
        {
            _appLifetime = appLifetime;
            _scopeAccessor = scopeAccessor;

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

        #region Run methods

        public Task Run(
            Action<ILifetimeScope, CancellationToken> action,
            TaskCreationOptions options = TaskCreationOptions.LongRunning,
            TaskScheduler scheduler = null,
            CancellationToken cancellationToken = default)
        {
            Guard.NotNull(action, nameof(action));

            var cancelToken = CreateCompositeCancellationToken(cancellationToken);

            var t = Task.Factory.StartNew(() =>
            {
                using var scope = CreateScope();
                action(scope, cancelToken);
            }, cancelToken, options, scheduler ?? TaskScheduler.Default);

            t.ContinueWith(t => TaskContinuation(t), cancelToken);

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

            var t = Task.Factory.StartNew((o) =>
            {
                using var scope = CreateScope();
                action(scope, cancelToken, o);
            }, state, cancelToken, options, scheduler ?? TaskScheduler.Default);

            t.ContinueWith(t => TaskContinuation(t), cancelToken);

            return t;
        }

        public Task<TResult> Run<TResult>(
            Func<ILifetimeScope, CancellationToken, TResult> function,
            TaskCreationOptions options = TaskCreationOptions.None,
            TaskScheduler scheduler = null,
            CancellationToken cancellationToken = default)
        {
            Guard.NotNull(function, nameof(function));

            var cancelToken = CreateCompositeCancellationToken(cancellationToken);

            var t = Task.Factory.StartNew(() =>
            {
                using var scope = CreateScope();
                return function(scope, cancelToken);
            }, cancelToken, options, scheduler ?? TaskScheduler.Default);

            t.ContinueWith(t => TaskContinuation(t), cancelToken);

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

            var t = Task.Factory.StartNew((o) =>
            {
                using var scope = CreateScope();
                return function(scope, cancelToken, o);
            }, state, cancelToken, options, scheduler ?? TaskScheduler.Default);

            t.ContinueWith(t => TaskContinuation(t), cancelToken);

            return t;
        }

        public Task RunTask(
            Func<ILifetimeScope, CancellationToken, Task> function,
            CancellationToken cancellationToken = default)
        {
            Guard.NotNull(function, nameof(function));

            var cancelToken = CreateCompositeCancellationToken(cancellationToken);

            var t = Task.Factory.StartNew(async () =>
            {
                using var scope = CreateScope();
                await function(scope, cancelToken);
            }, cancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

            t.ContinueWith(t => TaskContinuation(t), cancelToken);

            return t;
        }

        public Task<TResult> RunTask<TResult>(
            Func<ILifetimeScope, CancellationToken, Task<TResult>> function,
            CancellationToken cancellationToken = default)
        {
            Guard.NotNull(function, nameof(function));

            var cancelToken = CreateCompositeCancellationToken(cancellationToken);

            var t = Task.Factory.StartNew(async () =>
            {
                using var scope = CreateScope();
                return await function(scope, cancelToken);
            }, cancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

            t.ContinueWith(t => TaskContinuation(t), cancelToken);

            return t;
        }

        public Task RunTask(
            Func<ILifetimeScope, CancellationToken, object, Task> function,
            object state,
            CancellationToken cancellationToken = default)
        {
            Guard.NotNull(function, nameof(function));
            Guard.NotNull(state, nameof(state));

            var cancelToken = CreateCompositeCancellationToken(cancellationToken);

            var t = Task.Factory.StartNew(async (o) =>
            {
                using var scope = CreateScope();
                await function(scope, cancelToken, o);
            }, state, cancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

            t.ContinueWith(t => TaskContinuation(t), cancelToken);

            return t;
        }

        private ILifetimeScope CreateScope()
        {
            var scope = _scopeAccessor.CreateLifetimeScope();
            _scopeAccessor.LifetimeScope = scope;

            return scope;
        }

        private void TaskContinuation(Task task)
        {
            if (task.Exception != null)
            {
                task.Exception.Flatten().InnerExceptions.Each(x => Logger.Error(x));
            }
        }

        #endregion
    }
}