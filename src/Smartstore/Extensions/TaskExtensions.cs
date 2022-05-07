using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Smartstore
{
    public static partial class TaskExtensions
    {
        /// <summary>
        /// Awaits a task synchronously. 
        /// Shortcut for <code>task.GetAwaiter().GetResult()</code>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static void Await(this Task task)
        {
            task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Awaits a task synchronously and returns the result. 
        /// Shortcut for <code>task.GetAwaiter().GetResult()</code>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static T Await<T>(this Task<T> task)
        {
            return task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Awaits a task synchronously. 
        /// Shortcut for <code>task.GetAwaiter().GetResult()</code>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static void Await(this ValueTask task)
        {
            task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Awaits a task synchronously and returns the result. 
        /// Shortcut for <code>task.GetAwaiter().GetResult()</code>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static T Await<T>(this ValueTask<T> task)
        {
            return task.GetAwaiter().GetResult();
        }
    }
}
