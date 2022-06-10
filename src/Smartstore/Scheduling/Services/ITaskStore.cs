namespace Smartstore.Scheduling
{
    /// <summary>
    /// Storage for <see cref="TaskDescriptor"/> instances.
    /// </summary>
    public partial interface ITaskStore
    {
        /// <summary>
        /// Creates and returns an <see cref="IQueryable{TaskDescriptor}"/> query instance used to query over <see cref="TaskDescriptor"/> object instances.
        /// </summary>
        /// <returns>The queryable</returns>
        IQueryable<TaskDescriptor> GetDescriptorQuery();

        /// <summary>
        /// Creates a fresh store specific <see cref="TaskDescriptor"/> instance.
        /// <see cref="TaskDescriptor.Enabled"/> will be <c>true</c>.
        /// </summary>
        /// <param name="name">Unique name of task descriptor</param>
        /// <param name="type">Impl type of runnable task. Must implement <see cref="ITask"/>.</param>
        /// <returns>The fresh <see cref="TaskDescriptor"/> object instance.</returns>
        TaskDescriptor CreateDescriptor(string name, Type type);

        /// <summary>
        /// Gets a task by identifier.
        /// </summary>
        /// <param name="taskId">Task identifier</param>
        Task<TaskDescriptor> GetTaskByIdAsync(int taskId);

        /// <summary>
        /// Gets a task by its type.
        /// </summary>
        /// <param name="type">Task type</param>
        Task<TaskDescriptor> GetTaskByTypeAsync(string type);

        /// <summary>
        /// Reloads a task from the store overwriting any property values with values from the store.
        /// </summary>
        /// <param name="task">Task to reload</param>
        Task ReloadTaskAsync(TaskDescriptor task);

        /// <summary>
        /// Gets all tasks.
        /// </summary>
        /// <param name="includeDisabled">A value indicating whether to load disabled tasks also.</param>
        /// <param name="includeHidden">A value indicating whether to load hidden tasks also.</param>
        Task<List<TaskDescriptor>> GetAllTasksAsync(bool includeDisabled = false, bool includeHidden = false);

        /// <summary>
        /// Gets all currently pending tasks.
        /// </summary>
        Task<List<TaskDescriptor>> GetPendingTasksAsync();

        /// <summary>
        /// Inserts a task to the store.
        /// </summary>
        /// <param name="task">Task</param>
        Task InsertTaskAsync(TaskDescriptor task);

        /// <summary>
        /// Updates a task.
        /// </summary>
        /// <param name="task">Task</param>
        Task UpdateTaskAsync(TaskDescriptor task);

        /// <summary>
        /// Deletes a task from the store.
        /// </summary>
        /// <param name="task">Task</param>
        Task DeleteTaskAsync(TaskDescriptor task);

        /// <summary>
        /// Inserts a new task definition to the database or returns an existing one
        /// </summary>
        /// <typeparam name="T">The concrete implementation of the task</typeparam>
        /// <param name="createAction">Wraps the newly created <see cref="TaskDescriptor"/> instance</param>
        /// <returns>A newly created or existing task instance</returns>
        /// <remarks>
        /// This method does NOT update an already exising task
        /// </remarks>
        Task<TaskDescriptor> GetOrAddTaskAsync<T>(Action<TaskDescriptor> createAction) where T : ITask;

        /// <summary>
        /// Calculates - according to their cron expressions - all task future schedules
        /// and saves them to the store.
        /// </summary>
        /// <param name="isAppStart">When <c>true</c>, determines stale tasks and fixes their states to idle.</param>
        Task CalculateFutureSchedulesAsync(IEnumerable<TaskDescriptor> tasks, bool isAppStart = false);

        /// <summary>
        /// Calculates the next schedule according to the task's cron expression
        /// </summary>
        /// <param name="task">ScheduleTask</param>
        /// <returns>The next schedule or <c>null</c> if the task is disabled</returns>
        DateTime? GetNextSchedule(TaskDescriptor task);

        #region History

        /// <summary>
        /// Creates and returns an <see cref="IQueryable{TaskExecutionInfo}"/> query instance used to query over <see cref="TaskExecutionInfo"/> object instances.
        /// </summary>
        /// <returns>The queryable</returns>
        IQueryable<TaskExecutionInfo> GetExecutionInfoQuery(bool tracked = true);

        /// <summary>
        /// Creates a fresh store specific <see cref="TaskExecutionInfo"/> instance.
        /// </summary>
        /// <param name="task">The task to create an <see cref="TaskExecutionInfo"/> object instance for.</param>
        /// <returns>The fresh <see cref="TaskExecutionInfo"/> object instance.</returns>
        TaskExecutionInfo CreateExecutionInfo(TaskDescriptor task);

        /// <summary>
        /// Gets a <see cref="TaskExecutionInfo"/> object instance by unique identifier, or <c>null</c> if entry does not exist.
        /// </summary>
        /// <param name="id">Unique identifier.</param>
        Task<TaskExecutionInfo> GetExecutionInfoByIdAsync(int id);

        /// <summary>
        /// Gets a task's last <see cref="TaskExecutionInfo"/> object instance, or <c>null</c> if entry does not exist.
        /// </summary>
        /// <param name="taskId">Task identifier.</param>
        /// <param name="runningOnly">Filter by running entries. null = don't care, true = only running infos, false = history only.</param>
        Task<TaskExecutionInfo> GetLastExecutionInfoByTaskIdAsync(int taskId, bool? runningOnly = null);

        /// <summary>
        /// Gets a task's last <see cref="TaskExecutionInfo"/> object instance, or <c>null</c> if entry does not exist.
        /// </summary>
        /// <param name="task">Task descriptor instance.</param>
        /// <param name="runningOnly">Filter by running entries. null = don't care, true = only running infos, false = history only.</param>
        Task<TaskExecutionInfo> GetLastExecutionInfoByTaskAsync(TaskDescriptor task, bool? runningOnly = null);

        /// <summary>
        /// Inserts an <see cref="TaskExecutionInfo"/> instance to the store.
        /// </summary>
        /// <param name="info">The entry to insert.</param>
        Task InsertExecutionInfoAsync(TaskExecutionInfo info);

        /// <summary>
        /// Updates an <see cref="TaskExecutionInfo"/> instance in the store.
        /// </summary>
        /// <param name="info">The entry to update.</param>
        Task UpdateExecutionInfoAsync(TaskExecutionInfo info);

        /// <summary>
        /// Deletes an <see cref="TaskExecutionInfo"/> instance from the store.
        /// </summary>
        /// <param name="info">The entry to delete.</param>
        Task DeleteExecutionInfoAsync(TaskExecutionInfo info);

        /// <summary>
        /// Deletes <see cref="TaskExecutionInfo"/> by identifier.
        /// </summary>
        /// <param name="ids"><see cref="TaskExecutionInfo"/> identifier.</param>
        /// <returns>Number of deleted entries.</returns>
        Task<int> DeleteExecutionInfosByIdsAsync(IEnumerable<int> ids);

        /// <summary>
        /// Deletes old <see cref="TaskExecutionInfo"/> instances from the store.
        /// </summary>
        /// <returns>Number of deleted entries.</returns>
        Task<int> TrimExecutionInfosAsync(CancellationToken cancelToken = default);

        #endregion
    }

    public static class ITaskStoreExtensions
    {
        /// <summary>
        /// Loads - if not already loaded - the last <see cref="TaskExecutionInfo"/> object instance for a task from the store
        /// and assigns data to <see cref="TaskDescriptor.LastExecution"/>.
        /// </summary>
        /// <param name="task">The task to load data for.</param>
        /// <param name="force"><c>true</c> always enforces a reload, even if data is loaded already.</param>
        public static async Task LoadLastExecutionInfoAsync(this ITaskStore store, TaskDescriptor task, bool force = false)
        {
            Guard.NotNull(task, nameof(task));

            if (task.LastExecution == null || force)
            {
                task.LastExecution = await store.GetLastExecutionInfoByTaskAsync(task);
            }
        }

        public static Task<TaskDescriptor> GetTaskByTypeAsync<T>(this ITaskStore store) where T : ITask
        {
            return store.GetTaskByTypeAsync(typeof(T));
        }

        public static Task<TaskDescriptor> GetTaskByTypeAsync(this ITaskStore store, Type taskType)
        {
            Guard.NotNull(taskType, nameof(taskType));

            var type = taskType.GetAttribute<TaskNameAttribute>(false)?.Name ?? taskType.Name;
            return store.GetTaskByTypeAsync(type);
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
