using Microsoft.EntityFrameworkCore;

namespace Smartstore
{
    public static class DbFunctionsExtensions
    {
        const string FuncOnClient = "The method '{0}' is for use with Entity Framework Core only and has no in-memory implementation.";

        #region DateDiffYear

        /// <summary>
        /// Counts the number of year boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of year boundaries crossed between the dates.</returns>
        public static int DateDiffYear(
            this DbFunctions _,
            DateTime startDate,
            DateTime endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffYear)));

        /// <summary>
        /// Counts the number of year boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of year boundaries crossed between the dates.</returns>
        public static int? DateDiffYear(
            this DbFunctions _,
            DateTime? startDate,
            DateTime? endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffYear)));

        /// <summary>
        /// Counts the number of year boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of year boundaries crossed between the dates.</returns>
        public static int DateDiffYear(
            this DbFunctions _,
            DateTimeOffset startDate,
            DateTimeOffset endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffYear)));

        /// <summary>
        /// Counts the number of year boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of year boundaries crossed between the dates.</returns>
        public static int? DateDiffYear(
            this DbFunctions _,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffYear)));

        #endregion

        #region DateDiffMonth

        /// <summary>
        /// Counts the number of month boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of month boundaries crossed between the dates.</returns>
        public static int DateDiffMonth(
            this DbFunctions _,
            DateTime startDate,
            DateTime endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffMonth)));

        /// <summary>
        /// Counts the number of month boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of month boundaries crossed between the dates.</returns>
        public static int? DateDiffMonth(
            this DbFunctions _,
            DateTime? startDate,
            DateTime? endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffMonth)));

        /// <summary>
        /// Counts the number of month boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of month boundaries crossed between the dates.</returns>
        public static int DateDiffMonth(
            this DbFunctions _,
            DateTimeOffset startDate,
            DateTimeOffset endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffMonth)));

        /// <summary>
        /// Counts the number of month boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of month boundaries crossed between the dates.</returns>
        public static int? DateDiffMonth(
            this DbFunctions _,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffMonth)));

        #endregion

        #region DateDiffDay

        /// <summary>
        /// Counts the number of day boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of day boundaries crossed between the dates.</returns>
        public static int DateDiffDay(
            this DbFunctions _,
            DateTime startDate,
            DateTime endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffDay)));

        /// <summary>
        /// Counts the number of day boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of day boundaries crossed between the dates.</returns>
        public static int? DateDiffDay(
            this DbFunctions _,
            DateTime? startDate,
            DateTime? endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffDay)));

        /// <summary>
        /// Counts the number of day boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of day boundaries crossed between the dates.</returns>
        public static int DateDiffDay(
            this DbFunctions _,
            DateTimeOffset startDate,
            DateTimeOffset endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffDay)));

        /// <summary>
        /// Counts the number of day boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of day boundaries crossed between the dates.</returns>
        public static int? DateDiffDay(
            this DbFunctions _,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffDay)));

        #endregion

        #region DateDiffHour

        /// <summary>
        /// Counts the number of hour boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of hour boundaries crossed between the dates.</returns>
        public static int DateDiffHour(
            this DbFunctions _,
            DateTime startDate,
            DateTime endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffHour)));

        /// <summary>
        /// Counts the number of hour boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of hour boundaries crossed between the dates.</returns>
        public static int? DateDiffHour(
            this DbFunctions _,
            DateTime? startDate,
            DateTime? endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffHour)));

        /// <summary>
        /// Counts the number of hour boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of hour boundaries crossed between the dates.</returns>
        public static int DateDiffHour(
            this DbFunctions _,
            DateTimeOffset startDate,
            DateTimeOffset endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffHour)));

        /// <summary>
        /// Counts the number of hour boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of hour boundaries crossed between the dates.</returns>
        public static int? DateDiffHour(
            this DbFunctions _,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffHour)));

        #endregion

        #region DateDiffMinute

        /// <summary>
        /// Counts the number of minute boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of minute boundaries crossed between the dates.</returns>
        public static int DateDiffMinute(
            this DbFunctions _,
            DateTime startDate,
            DateTime endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffMinute)));

        /// <summary>
        /// Counts the number of minute boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of minute boundaries crossed between the dates.</returns>
        public static int? DateDiffMinute(
            this DbFunctions _,
            DateTime? startDate,
            DateTime? endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffMinute)));

        /// <summary>
        /// Counts the number of minute boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of minute boundaries crossed between the dates.</returns>
        public static int DateDiffMinute(
            this DbFunctions _,
            DateTimeOffset startDate,
            DateTimeOffset endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffMinute)));

        /// <summary>
        /// Counts the number of minute boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of minute boundaries crossed between the dates.</returns>
        public static int? DateDiffMinute(
            this DbFunctions _,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffMinute)));

        #endregion

        #region DateDiffSecond

        /// <summary>
        /// Counts the number of second boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of second boundaries crossed between the dates.</returns>
        public static int DateDiffSecond(
            this DbFunctions _,
            DateTime startDate,
            DateTime endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffSecond)));

        /// <summary>
        /// Counts the number of second boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of second boundaries crossed between the dates.</returns>
        public static int? DateDiffSecond(
            this DbFunctions _,
            DateTime? startDate,
            DateTime? endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffSecond)));

        /// <summary>
        /// Counts the number of second boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of second boundaries crossed between the dates.</returns>
        public static int DateDiffSecond(
            this DbFunctions _,
            DateTimeOffset startDate,
            DateTimeOffset endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffSecond)));

        /// <summary>
        /// Counts the number of second boundaries crossed between the startDate and endDate.
        /// </summary>
        /// <param name="_">The DbFunctions instance.</param>
        /// <param name="startDate">Starting date for the calculation.</param>
        /// <param name="endDate">Ending date for the calculation.</param>
        /// <returns>Number of second boundaries crossed between the dates.</returns>
        public static int? DateDiffSecond(
            this DbFunctions _,
            DateTimeOffset? startDate,
            DateTimeOffset? endDate)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(DateDiffSecond)));

        #endregion
    }
}
