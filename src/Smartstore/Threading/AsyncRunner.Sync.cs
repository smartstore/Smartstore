using System.Collections.Concurrent;

namespace Smartstore.Threading
{
    using EventQueue = ConcurrentQueue<Tuple<SendOrPostCallback, object>>;

    public partial class AsyncRunner
    {
        /// <summary>
        /// Executes an async Task method which has a void return value synchronously
        /// </summary>
        /// <param name="func">Task method to execute</param>
        public static void RunSync(Func<Task> func, Action<Task> continuation = null)
        {
            using (var runner = new SyncRunnerScope())
            {
                runner.RunSync(func(), continuation);
            }
        }

        /// <summary>
        /// Executes an async Task method which has a TResult return type synchronously
        /// </summary>
        /// <typeparam name="TResult">Return Type</typeparam>
        /// <param name="func">Task method to execute</param>
        /// <returns></returns>
        public static TResult RunSync<TResult>(Func<Task<TResult>> func, Action<Task<TResult>> continuation = null)
        {
            var result = default(TResult);

            using (var runner = new SyncRunnerScope())
            {
                result = runner.RunSync(func(), continuation);
            }

            return result;
        }

        private class SyncRunnerScope : Disposable
        {
            private readonly ExclusiveSynchronizationContext _currentContext;
            private readonly SynchronizationContext _oldContext;
            private int _taskCount;

            public SyncRunnerScope()
            {
                _oldContext = SynchronizationContext.Current;
                _currentContext = new ExclusiveSynchronizationContext(_oldContext);
                SynchronizationContext.SetSynchronizationContext(_currentContext);
            }

            private void Increment()
            {
                Interlocked.Increment(ref _taskCount);
            }

            private void Decrement()
            {
                Interlocked.Decrement(ref _taskCount);
                if (_taskCount == 0)
                {
                    _currentContext.EndMessageLoop();
                }
            }

            /// <summary>
            /// Executes an async Task method which has a void return value synchronously
            /// </summary>
            /// <param name="task">Task execute</param>
            public void RunSync(Task task, Action<Task> continuation = null)
            {
                _currentContext.Post(async _ =>
                {
                    try
                    {
                        Increment();
                        await task;

                        continuation?.Invoke(task);
                    }
                    catch (Exception e)
                    {
                        _currentContext.InnerException = e;
                    }
                    finally
                    {
                        Decrement();
                    }
                }, null);

                _currentContext.BeginMessageLoop();
            }

            /// <summary>
            /// Executes an async Task method which has a TResult return type synchronously
            /// </summary>
            /// <typeparam name="TResult">Return Type</typeparam>
            /// <param name="task">Task to execute</param>
            public TResult RunSync<TResult>(Task<TResult> task, Action<Task<TResult>> continuation = null)
            {
                var result = default(TResult);
                var curContext = _currentContext;

                curContext.Post(async _ =>
                {
                    try
                    {
                        Increment();
                        result = await task;

                        continuation?.Invoke(task);
                    }
                    catch (Exception e)
                    {
                        _currentContext.InnerException = e;
                    }
                    finally
                    {
                        Decrement();
                    }
                }, null);

                curContext.BeginMessageLoop();

                return result;
            }

            protected override void OnDispose(bool disposing)
            {
                SynchronizationContext.SetSynchronizationContext(_oldContext);
            }
        }

        private class ExclusiveSynchronizationContext : SynchronizationContext
        {
            private bool _done;
            private readonly AutoResetEvent _workItemsWaiting = new AutoResetEvent(false);
            private readonly EventQueue _items;

            public ExclusiveSynchronizationContext(SynchronizationContext old)
            {
                if (old is ExclusiveSynchronizationContext oldEx)
                {
                    this._items = oldEx._items;
                }
                else
                {
                    this._items = new EventQueue();
                }
            }

            public Exception InnerException { get; set; }

            public override void Send(SendOrPostCallback d, object state)
            {
                throw new NotSupportedException("We cannot send to the same thread");
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                _items.Enqueue(Tuple.Create(d, state));
                _workItemsWaiting.Set();
            }

            public void EndMessageLoop()
            {
                Post(_ => _done = true, null);
            }

            public void BeginMessageLoop()
            {
                while (!_done)
                {
                    if (!_items.TryDequeue(out var task))
                    {
                        _workItemsWaiting.WaitOne();
                    }
                    else
                    {
                        task.Item1(task.Item2);

                        if (InnerException != null) // method threw an exeption
                        {
                            throw new AggregateException("AsyncRunner.Run method threw an exception.", InnerException);
                        }
                    }
                }
            }

            public override SynchronizationContext CreateCopy()
            {
                return this;
            }
        }
    }
}