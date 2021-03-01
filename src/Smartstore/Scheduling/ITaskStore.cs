using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Smartstore.Scheduling
{
    // TODO: (core) ITaskStore: implement mapping between old/legacy and new task types. 

    /// <summary>
    /// Storage for <see cref="ITaskDescriptor"/> implementations.
    /// </summary>
    public interface ITaskStore
    {
        /// <summary>
        /// Gets the task CLR type.
        /// </summary>
        /// <param name="task">The task to map a CLR type for.</param>
        /// <returns>
        /// A <see cref="Type"/> instance representing the task CLR type or <c>null</c>.
        /// </returns>
        Type GetTaskClrType(ITaskDescriptor task);

        /// <summary>
        /// Gets a task by identifier.
        /// </summary>
        /// <param name="taskId">Task identifier</param>
        Task<ITaskDescriptor> GetTaskByIdAsync(int taskId);

        /// <summary>
        /// Gets a task by its type.
        /// </summary>
        /// <param name="type">Task type</param>
        Task<ITaskDescriptor> GetTaskByTypeAsync(string type);

        /// <summary>
        /// Reloads a task from the store overwriting any property values with values from the store.
        /// </summary>
        /// <param name="task">Task to reload</param>
        Task ReloadTaskAsync(ITaskDescriptor task);

        /// <summary>
        /// Gets all tasks
        /// </summary>
        /// <param name="includeDisabled">A value indicating whether to load disabled tasks also</param>
        Task<IEnumerable<ITaskDescriptor>> GetAllTasksAsync(bool includeDisabled = false);

        /// <summary>
        /// Gets all currently pending tasks.
        /// </summary>
        Task<IEnumerable<ITaskDescriptor>> GetPendingTasksAsync();

        /// <summary>
        /// Adds a task to the store.
        /// </summary>
        /// <param name="task">Task</param>
        Task AddTaskAsync(ITaskDescriptor task);

        /// <summary>
        /// Updates a task.
        /// </summary>
        /// <param name="task">Task</param>
        Task UpdateTaskAsync(ITaskDescriptor task);

        /// <summary>
        /// Deletes a task from the store.
        /// </summary>
        /// <param name="task">Task</param>
        Task DeleteTaskAsync(ITaskDescriptor task);

        /// <summary>
        /// Inserts a new task definition to the database or returns an existing one
        /// </summary>
        /// <typeparam name="T">The concrete implementation of the task</typeparam>
        /// <param name="action">Wraps the newly created <see cref="ITaskDescriptor"/> instance</param>
        /// <returns>A newly created or existing task instance</returns>
        /// <remarks>
        /// This method does NOT update an already exising task
        /// </remarks>
        Task<ITaskDescriptor> GetOrAddTaskAsync<T>(Action<ITaskDescriptor> newAction) where T : ITask;

        /// <summary>
        /// Calculates - according to their cron expressions - all task future schedules
        /// and saves them to the store.
        /// </summary>
        /// <param name="isAppStart">When <c>true</c>, determines stale tasks and fixes their states to idle.</param>
        Task CalculateFutureSchedulesAsync(IEnumerable<ITaskDescriptor> tasks, bool isAppStart = false);

        /// <summary>
        /// Calculates the next schedule according to the task's cron expression
        /// </summary>
        /// <param name="task">ScheduleTask</param>
        /// <returns>The next schedule or <c>null</c> if the task is disabled</returns>
        Task<DateTime?> GetNextScheduleAsync(ITaskDescriptor task);

        #region History

        ITaskExecutionInfo CreateExecutionInfo(ITaskDescriptor task);

        Task InsertExecutionInfoAsync(ITaskExecutionInfo info);

        Task UpdateExecutionInfoAsync(ITaskExecutionInfo info);

        Task DeleteExecutionInfoAsync(ITaskExecutionInfo info);

        Task LoadLastExecutionInfoAsync(ITaskDescriptor task);

        Task<int> TrimExecutionInfosAsync();

        // TODO: (core) Continue ...

        #endregion
    }

    public static class ITaskStoreExtensions
    {
        public static Task<ITaskDescriptor> GetTaskByTypeAsync<T>(this ITaskStore store) where T : ITask
        {
            return store.GetTaskByTypeAsync(typeof(T));
        }

        public static Task<ITaskDescriptor> GetTaskByTypeAsync(this ITaskStore store, Type taskType)
        {
            Guard.NotNull(taskType, nameof(taskType));

            return store.GetTaskByTypeAsync(taskType.AssemblyQualifiedNameWithoutVersion());
        }

        public static async Task<bool> TryDeleteTaskAsync<T>(this ITaskStore store) where T : ITask
        {
            var task = await store.GetTaskByTypeAsync(typeof(T));

            if (task != null)
            {
                await store.DeleteTaskAsync(task);
                return true;
            }

            return false;
        }
    }
}
