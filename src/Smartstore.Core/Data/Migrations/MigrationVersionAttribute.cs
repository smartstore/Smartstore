using System.Globalization;
using FluentMigrator;

namespace Smartstore.Core.Data.Migrations
{
    /// <summary>
    /// Specifies the version of a database migration.
    /// </summary>
    public sealed class MigrationVersionAttribute : MigrationAttribute
    {
        private static readonly string[] _timestampFormats = new[]
        {
            "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd HH:mm:ss", "yyyy.MM.dd HH:mm:ss"
        };

        /// <summary>
        /// Specifies the version of a database migration.
        /// </summary>
        /// <param name="timestamp">yyyy-MM-dd HH:mm:ss formatted timestamp to get the version from, e.g. "2021-08-18 15:45:35.</param>
        /// <param name="description">Optional, short decription of the migration.</param>
        public MigrationVersionAttribute(string timestamp, string description = null, TransactionBehavior transactionBehavior = TransactionBehavior.Default)
            : base(GetVersion(timestamp), transactionBehavior, BuildDescription(description))
        {
        }

        /// <summary>
        /// Gets the migration version from a timestamp.
        /// Supported timestamp formats are: yyyy-MM-dd HH:mm:ss, yyyy/MM/dd HH:mm:ss, yyyy.MM.dd HH:mm:ss.
        /// </summary>
        /// <param name="timestamp">yyyy-MM-dd HH:mm:ss formatted timestamp to get the version from, e.g. "2021-08-18 15:45:35.</param>
        /// <returns>Migration version.</returns>
        public static long GetVersion(string timestamp)
        {
            Guard.NotEmpty(timestamp, nameof(timestamp));

            if (DateTime.TryParseExact(timestamp, _timestampFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt.Ticks;
            }
            else
            {
                throw new ArgumentException($"Cannot get migration version because of unsupported timestamp format '{timestamp}'. Please use one of these formats: {string.Join(", ", _timestampFormats)}.");
            }
        }

        private static string BuildDescription(string description)
        {
            Guard.NotEmpty(description, nameof(description));

            return SmartstoreVersion.CurrentFullVersion.Grow(description);
        }
    }
}
