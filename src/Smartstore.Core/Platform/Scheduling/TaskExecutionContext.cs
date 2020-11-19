using System;
using System.Threading.Tasks;

namespace Smartstore.Core.Scheduling
{
    public delegate Task ProgressCallback(int value, int maximum, string message);

    /// <summary>
    /// Provides the context for the Execute method of the <see cref="ITask"/> interface.
    /// </summary>
    public class TaskExecutionContext
    {
        // TODO: (core) Implement TaskExecutionContext class

        /// <summary>
        /// Persists a task's progress information to the database
        /// </summary>
        /// <param name="value">Progress value (numerator)</param>
        /// <param name="maximum">Progress maximum (denominator)</param>
        /// <param name="message">Progress message. Can be <c>null</c>.</param>
        /// <param name="immediately">if <c>true</c>, saves the updated task entity immediately, or lazily with the next database commit otherwise.</param>
        public Task SetProgressAsync(int value, int maximum, string message, bool immediately = false)
        {
            if (value == 0 && maximum == 0)
            {
                return SetProgressAsync(null, message, immediately);
            }
            else
            {
                float fraction = (float)value / (float)Math.Max(maximum, 1f);
                int percentage = (int)Math.Round(fraction * 100f, 0);

                return SetProgressAsync(Math.Min(Math.Max(percentage, 0), 100), message, immediately);
            }
        }

        /// <summary>
        /// Persists a task's progress information to the database
        /// </summary>
        /// <param name="progress">Percentual progress. Can be <c>null</c> or a value between 0 and 100.</param>
        /// <param name="message">Progress message. Can be <c>null</c>.</param>
        /// <param name="immediately">if <c>true</c>, saves the updated task entity immediately, or lazily with the next database commit otherwise.</param>
        public virtual Task SetProgressAsync(int? progress, string message, bool immediately = false)
        {
            if (progress.HasValue)
            {
                Guard.InRange(progress.Value, 0, 100, nameof(progress));
            }

            // TODO: (core) Implement this once ScheduleTaskHistory is available
            //// Update cloned entity.
            //ScheduleTaskHistory.ProgressPercent = progress;
            //ScheduleTaskHistory.ProgressMessage = message;

            //// Update attached entity.
            //_originalTaskHistory.ProgressPercent = progress;
            //_originalTaskHistory.ProgressMessage = message;

            //if (immediately)
            //{
            //    // Dont't let this abort the task on failure.
            //    try
            //    {
            //        var dbContext = _componentContext.Resolve<IDbContext>();
            //        //dbContext.ChangeState(_originalTask, System.Data.Entity.EntityState.Modified);
            //        dbContext.SaveChanges();
            //    }
            //    catch { }
            //}

            return Task.CompletedTask;
        }
    }
}
