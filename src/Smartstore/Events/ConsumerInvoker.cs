using System.Reflection;
using Autofac;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.ComponentModel;
using Smartstore.Threading;

namespace Smartstore.Events
{
    public class ConsumerInvoker : IConsumerInvoker
    {
        private readonly IConsumerResolver _resolver;
        private readonly AsyncRunner _asyncRunner;

        public ConsumerInvoker(IConsumerResolver resolver, AsyncRunner asyncRunner)
        {
            _resolver = resolver;
            _asyncRunner = asyncRunner;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual Task InvokeAsync<TMessage>(
            ConsumerDescriptor descriptor,
            IConsumer consumer,
            ConsumeContext<TMessage> envelope,
            CancellationToken cancelToken = default) where TMessage : class
        {
            var d = descriptor;
            var p = descriptor.WithEnvelope ? (object)envelope : envelope.Message;
            var invoker = FastInvoker.GetInvoker(d.Method);
            var ct = cancelToken;

            Task task;

            if (d.IsAsync && !d.FireForget)
            {
                // The all async case.
                ct = _asyncRunner.CreateCompositeCancellationToken(cancelToken);
                task = ((Task)InvokeCore(null, ct));
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted) HandleException(t.Exception, d);
                }, TaskContinuationOptions.None);
            }
            else if (d.FireForget)
            {
                // Sync or Async without await. Needs new dependency scope.
                task = d.IsAsync
                    ? _asyncRunner.RunTask((scope, ct) => ((Task)InvokeCore(scope, ct)))
                    : _asyncRunner.Run((scope, ct) => InvokeCore(scope, ct));
                task.ConfigureAwait(false);
            }
            else
            {
                // The all sync case
                try
                {
                    InvokeCore(null, ct);
                }
                catch (Exception ex)
                {
                    HandleException(ex, d);
                }

                task = Task.CompletedTask;
            }

            return task;

            object InvokeCore(IComponentContext c = null, CancellationToken cancelToken = default)
            {
                if (d.Parameters.Length == 0)
                {
                    // Only one method param: the message!
                    return invoker.Invoke(consumer, p);
                }

                var parameters = new object[d.Parameters.Length + 1];
                parameters[0] = p;

                int i = 0;
                foreach (var obj in ResolveParameters(c, d, cancelToken).ToArray())
                {
                    i++;
                    parameters[i] = obj;
                }

                return invoker.Invoke(consumer, parameters);
            }
        }

        protected internal virtual IEnumerable<object> ResolveParameters(
            IComponentContext container,
            ConsumerDescriptor descriptor,
            CancellationToken cancelToken)
        {
            foreach (var p in descriptor.Parameters)
            {
                if (p.ParameterType == typeof(CancellationToken))
                {
                    yield return cancelToken;
                }
                else
                {
                    yield return _resolver.ResolveParameter(p, container);
                }
            }
        }

        protected virtual void HandleException(Exception ex, ConsumerDescriptor descriptor)
        {
            if (ex == null)
            {
                return;
            }

            if (ex is AggregateException ae)
            {
                ae.Flatten().InnerExceptions.Each(x => Logger.Error(x));
            }
            else
            {
                Logger.Error(ex);
            }

            if (!descriptor.FireForget)
            {
                ex.ReThrow();
            }
        }

        #region APM > TPL pattern (obsolete, does not always work stable)

        ///// <summary>
        ///// Wraps a <see cref="Task"/> into the Begin method of an APM pattern.
        ///// </summary>
        ///// <param name="task">The task to wrap.</param>
        ///// <param name="callback">The callback method passed into the Begin method of the APM pattern.</param>
        ///// <param name="descriptor">The state passed into the Begin method of the APM pattern.</param>
        ///// <returns>The asynchronous operation, to be returned by the Begin method of the APM pattern.</returns>
        //protected IAsyncResult BeginInvoke(Task task, AsyncCallback callback, ConsumerDescriptor descriptor)
        //{
        //    var options = TaskCreationOptions.RunContinuationsAsynchronously;
        //    var tcs = new TaskCompletionSource<object>(descriptor, options);

        //    // "_ =" to discard 'async/await' compiler warning
        //    _ = AwaitCompletionAsync(task, callback, tcs, descriptor);

        //    return tcs.Task;
        //}

        //private async Task AwaitCompletionAsync(
        //    Task task,
        //    AsyncCallback callback,
        //    TaskCompletionSource<object> tcs,
        //    ConsumerDescriptor descriptor)
        //{
        //    try
        //    {
        //        await task;
        //        tcs.TrySetResult(null);
        //    }
        //    catch (OperationCanceledException ex)
        //    {
        //        tcs.TrySetCanceled(ex.CancellationToken);
        //    }
        //    catch (Exception ex)
        //    {
        //        tcs.TrySetException(ex);
        //    }
        //    finally
        //    {
        //        callback?.Invoke(tcs.Task);
        //    }
        //}

        //protected virtual void EndInvoke(IAsyncResult asyncResult)
        //{
        //    var task = (Task)asyncResult;

        //    if (task.IsFaulted && task.Exception != null)
        //    {
        //        HandleException(task.Exception, (ConsumerDescriptor)asyncResult.AsyncState);
        //    }
        //}

        #endregion
    }
}