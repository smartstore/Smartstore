using Autofac;
using Microsoft.AspNetCore.Http;

namespace Smartstore.Scheduling
{
    public delegate Task ProgressCallback(int value, int maximum, string message);

    /// <summary>
    /// Provides the context for the Execute method of the <see cref="ITask"/> interface.
    /// </summary>
    public class TaskExecutionContext
    {
        private readonly IComponentContext _componentContext;
        private readonly TaskExecutionInfo _originalExecutionInfo;

        public TaskExecutionContext(
            ITaskStore taskStore,
            HttpContext httpContext,
            IComponentContext componentContext,
            TaskExecutionInfo originalExecutionInfo,
            IDictionary<string, string> taskParameters = null)
        {
            Guard.NotNull(taskStore, nameof(taskStore));
            Guard.NotNull(httpContext, nameof(httpContext));
            Guard.NotNull(componentContext, nameof(componentContext));
            Guard.NotNull(originalExecutionInfo, nameof(originalExecutionInfo));

            _componentContext = componentContext;
            _originalExecutionInfo = originalExecutionInfo;

            HttpContext = httpContext;
            Parameters = taskParameters ?? new Dictionary<string, string>();
            TaskStore = taskStore;
            // TODO: (core) Maybe this is the cause why entity isn't attached.
            ExecutionInfo = _originalExecutionInfo.Clone();
        }

        public HttpContext HttpContext { get; }

        public IDictionary<string, string> Parameters { get; set; }

        /// <summary>
        /// The task store.
        /// </summary>
        public ITaskStore TaskStore { get; }

        /// <summary>
        /// The cloned execution info.
        /// </summary>
        public TaskExecutionInfo ExecutionInfo { get; }

        public T Resolve<T>(object key = null) where T : class
        {
            return key == null
                ? _componentContext.Resolve<T>()
                : _componentContext.ResolveKeyed<T>(key);
        }

        public T ResolveNamed<T>(string name) where T : class
        {
            return _componentContext.ResolveNamed<T>(name);
        }

        /// <summary>
        /// Persists a task's progress information to the store
        /// </summary>
        /// <param name="value">Progress value (numerator)</param>
        /// <param name="maximum">Progress maximum (denominator)</param>
        /// <param name="message">Progress message. Can be <c>null</c>.</param>
        public void SetProgress(int value, int maximum, string message)
            => SetProgressAsync(value, maximum, message).Await();

        /// <summary>
        /// Persists a task's progress information to the store
        /// </summary>
        /// <param name="value">Progress value (numerator)</param>
        /// <param name="maximum">Progress maximum (denominator)</param>
        /// <param name="message">Progress message. Can be <c>null</c>.</param>
        public Task SetProgressAsync(int value, int maximum, string message)
        {
            if (value == 0 && maximum == 0)
            {
                return SetProgressAsync(null, message);
            }
            else
            {
                float fraction = (float)value / (float)Math.Max(maximum, 1f);
                int percentage = (int)Math.Round(fraction * 100f, 0);

                return SetProgressAsync(Math.Min(Math.Max(percentage, 0), 100), message);
            }
        }

        /// <summary>
        /// Persists a task's progress information to the task store
        /// </summary>
        /// <param name="progress">Percentual progress. Can be <c>null</c> or a value between 0 and 100.</param>
        /// <param name="message">Progress message. Can be <c>null</c>.</param>
        public virtual void SetProgress(int? progress, string message)
            => SetProgressAsync(progress, message).Await();

        /// <summary>
        /// Persists a task's progress information to the task store
        /// </summary>
        /// <param name="progress">Percentual progress. Can be <c>null</c> or a value between 0 and 100.</param>
        /// <param name="message">Progress message. Can be <c>null</c>.</param>
        public virtual async Task SetProgressAsync(int? progress, string message)
        {
            if (progress.HasValue)
            {
                Guard.InRange(progress.Value, 0, 100, nameof(progress));
            }

            // Update cloned task.
            ExecutionInfo.ProgressPercent = progress;
            ExecutionInfo.ProgressMessage = message;

            // Update original task.
            _originalExecutionInfo.ProgressPercent = progress;
            _originalExecutionInfo.ProgressMessage = message;

            // Dont't let this abort the task on failure.
            try
            {
                await TaskStore.UpdateExecutionInfoAsync(_originalExecutionInfo);
            }
            catch
            {
            }
        }
    }
}
