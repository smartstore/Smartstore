namespace Smartstore.Scheduling
{
    internal class TaskMetadata
    {
        public string Name { get; set; }
        public Type Type { get; set; }
    }

    /// <summary>
    /// Represents a scheduled background task.
    /// </summary>
    public partial interface ITask
    {
        /// <summary>
        /// Runs a task implementation.
        /// </summary>
		/// <param name="ctx">The execution context</param>
        Task Run(TaskExecutionContext ctx, CancellationToken cancelToken = default);
    }
}