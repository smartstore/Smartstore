using System.Runtime.CompilerServices;

namespace Smartstore.Threading
{
    /// <summary>
    /// A <see cref="Lazy{T}"/> that initializes asynchronously and whose 
    /// <see cref="Lazy{T}.Value"/> can be awaited for initialization completion.
    /// </summary>
    /// <code>
    /// var lazy = new AsyncLazy&lt;T&gt;(() => ...);
    /// var value = await lazy.Value;
    /// </code>
    /// <typeparam name="T">The type of async lazily-initialized value.</typeparam>
    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        /// <summary>
        /// Initializes the lazy, using <see cref="Task.Run(Func{T})"/> to asynchronously 
        /// schedule the value factory execution.
        /// </summary>
        public AsyncLazy(Func<T> valueFactory)
            : base(() => Task.Run(valueFactory))
        {
        }

        /// <summary>
        /// Initializes the lazy, using <see cref="Task.Run(Func{Task{T}})"/> to asynchronously 
        /// schedule the value factory execution.
        /// </summary>
        public AsyncLazy(Func<Task<T>> taskFactory)
            : base(() => Task.Factory.StartNew(() => taskFactory()).Unwrap())
        {
        }

        public TaskAwaiter<T> GetAwaiter()
            => Value.GetAwaiter();
    }
}
