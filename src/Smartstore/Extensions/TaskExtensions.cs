using System.Threading.Tasks;

namespace Smartstore
{
    public static partial class TaskExtensions
    {
        /// <summary>
        /// Awaits a task synchronously. 
        /// Shortcut for <code>task.GetAwaiter().GetResult()</code>.
        /// </summary>
        public static void Await(this Task task)
        {
            task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Awaits a task synchronously and returns the result. 
        /// Shortcut for <code>task.GetAwaiter().GetResult()</code>.
        /// </summary>
        public static T Await<T>(this Task<T> task)
        {
            return task.GetAwaiter().GetResult();
        }
    }
}
