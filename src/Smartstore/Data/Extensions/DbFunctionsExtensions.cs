using System;
using Microsoft.EntityFrameworkCore;

namespace Smartstore
{
    // TODO: (core) DbFunctionsExtensions: find a way to delegate these functions to specific translators in provider assemblies.

    public static class DbFunctionsExtensions
    {
        const string FuncOnClient = "The method '{0}' is for use with Entity Framework Core only and has no in-memory implementation.";
        
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

        /// <summary>
        /// Returns the given date with the time portion cleared.
        /// </summary>
        /// <param name="dateValue">The date/time value to use.</param>
        /// <returns>The input date with the time portion cleared.</returns>
        public static DateTime? TruncateTime(
            this DbFunctions _,
            DateTime? dateValue)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(TruncateTime)));

        /// <summary>
        /// Returns the given date with the time portion cleared.
        /// </summary>
        /// <param name="dateValue">The date/time value to use.</param>
        /// <returns>The input date with the time portion cleared.</returns>
        public static DateTimeOffset? TruncateTime(
            this DbFunctions _,
            DateTimeOffset? dateValue)
            => throw new InvalidOperationException(FuncOnClient.FormatInvariant(nameof(TruncateTime)));
    }
}
