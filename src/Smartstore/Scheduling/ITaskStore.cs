using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Collections;

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
        /// Inserts a task to the store.
        /// </summary>
        /// <param name="task">Task</param>
        Task InsertTaskAsync(ITaskDescriptor task);

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
        /// <param name="createAction">Wraps the newly created <see cref="ITaskDescriptor"/> instance</param>
        /// <returns>A newly created or existing task instance</returns>
        /// <remarks>
        /// This method does NOT update an already exising task
        /// </remarks>
        Task<ITaskDescriptor> GetOrAddTaskAsync<T>(Action<ITaskDescriptor> createAction) where T : ITask;

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
        DateTime? GetNextSchedule(ITaskDescriptor task);

        #region History

        /// <summary>
        /// Creates and returns an <see cref="IQueryable{ITaskExecutionInfo}"/> query instance used query over <see cref="ITaskExecutionInfo"/> object instances.
        /// </summary>
        /// <returns>The queryable</returns>
        IQueryable<ITaskExecutionInfo> GetExecutionInfoQuery();

        /// <summary>
        /// Creates a fresh store specific <see cref="ITaskExecutionInfo"/> instance.
        /// </summary>
        /// <param name="task">The task to create an <see cref="ITaskExecutionInfo"/> object instance for.</param>
        /// <returns>The fresh <see cref="ITaskExecutionInfo"/> object instance.</returns>
        ITaskExecutionInfo CreateExecutionInfo(ITaskDescriptor task);

        /// <summary>
        /// Gets a <see cref="ITaskExecutionInfo"/> object instance by unique identifier, or <c>null</c> if entry does not exist.
        /// </summary>
        /// <param name="id">Unique identifier.</param>
        Task<ITaskExecutionInfo> GetExecutionInfoByIdAsync(int id);

        /// <summary>
        /// Gets a task's last <see cref="ITaskExecutionInfo"/> object instance, or <c>null</c> if entry does not exist.
        /// </summary>
        /// <param name="taskId">Task identifier.</param>
        /// <param name="runningOnly">Filter by running entries.</param>
        Task<ITaskExecutionInfo> GetLastExecutionInfoByTaskIdAsync(int taskId, bool? runningOnly = null);

        /// <summary>
        /// Gets a task's last <see cref="ITaskExecutionInfo"/> object instance, or <c>null</c> if entry does not exist.
        /// </summary>
        /// <param name="task">Task instance.</param>
        /// <param name="runningOnly">Filter by running entries.</param>
        Task<ITaskExecutionInfo> GetLastExecutionInfoByTaskAsync(ITask task, bool? runningOnly = null);

        /// <summary>
        /// Loads - if not already loaded - the last <see cref="ITaskExecutionInfo"/> object instance for a task from the store
        /// and assigns data to <see cref="ITaskDescriptor.LastExecution"/>.
        /// </summary>
        /// <param name="task">The task to load data for.</param>
        /// <param name="force"><c>true</c> always enforces a reload, even if data is loaded already.</param>
        Task LoadLastExecutionInfoAsync(ITaskDescriptor task, bool force = false);

        /// <summary>
        /// Inserts an <see cref="ITaskExecutionInfo"/> instance to the store.
        /// </summary>
        /// <param name="info">The entry to insert.</param>
        Task InsertExecutionInfoAsync(ITaskExecutionInfo info);

        /// <summary>
        /// Updates an <see cref="ITaskExecutionInfo"/> instance in the store.
        /// </summary>
        /// <param name="info">The entry to update.</param>
        Task UpdateExecutionInfoAsync(ITaskExecutionInfo info);

        /// <summary>
        /// Deletes an <see cref="ITaskExecutionInfo"/> instance from the store.
        /// </summary>
        /// <param name="info">The entry to delete.</param>
        Task DeleteExecutionInfoAsync(ITaskExecutionInfo info);

        /// <summary>
        /// Deletes old <see cref="ITaskExecutionInfo"/> instances from the store.
        /// </summary>
        /// <returns>Number of deleted entries.</returns>
        Task<int> TrimExecutionInfosAsync();

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
