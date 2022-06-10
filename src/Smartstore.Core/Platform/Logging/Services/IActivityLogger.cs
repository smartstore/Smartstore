using Smartstore.Core.Identity;

namespace Smartstore.Core.Logging
{
    /// <summary>
    /// Responsible for logging customer activities
    /// </summary>
    public partial interface IActivityLogger
    {
        /// <summary>
        /// Gets all activity log type entities as untracked from cache.
        /// </summary>
        IEnumerable<ActivityLogType> GetAllActivityTypes();

        /// <summary>
        /// Gets an activity log type as untracked by its system keyword from cache.
        /// </summary>
        /// <param name="keyword">The log type's system keyword</param>
        ActivityLogType GetActivityTypeByKeyword(string keyword);

        /// <summary>
        /// Logs a customer activity by automatically resolving the current customer. This method does NOT commit to database.
        /// </summary>
        /// <param name="activity">The system keyword of the activity.</param>
        /// <param name="comment">The activity comment</param>
        /// <param name="commentParams">The activity comment parameters to format <paramref name="comment"/> with.</param>
        /// <returns>Transient activity log item</returns>
        ActivityLog LogActivity(string activity, string comment, params object[] commentParams);

        /// <summary>
        /// Logs a customer activity. This method does NOT commit to database.
        /// </summary>
        /// <param name="activity">The system keyword of the activity.</param>
        /// <param name="comment">The activity comment</param>
        /// <param name="customer">The customer who performs the activity.</param>
        /// <param name="commentParams">The activity comment parameters to format <paramref name="comment"/> with.</param>
        /// <returns>Transient activity log item</returns>
        ActivityLog LogActivity(string activity, string comment, Customer customer, params object[] commentParams);

        /// <summary>
        /// Clears ALL activities from database by TRUNCATING the table and resetting the id increment. THINK TWICE!!
        /// </summary>
        Task ClearAllActivitiesAsync(CancellationToken cancelToken = default);
    }
}
