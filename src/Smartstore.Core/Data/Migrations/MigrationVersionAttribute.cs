using System;
using System.Globalization;
using FluentMigrator;

namespace Smartstore.Core.Data.Migrations
{
    /// <summary>
    /// Defines the version of a migration based on a yyyy-MM-dd HH:mm:ss formatted timestamp, e.g. "2021-08-18 15:45:35".
    /// </summary>
    public sealed class MigrationVersionAttribute : MigrationAttribute
    {
        private static readonly string[] _timestampFormats = new[]
        { 
            "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd HH:mm:ss", "yyyy.MM.dd HH:mm:ss"
        };

        public MigrationVersionAttribute(string timestamp, string description = null)
            : base(GetVersion(timestamp), BuildDescription(description))
        {
        }

        public static long GetVersion(string timestamp)
        {
            Guard.NotEmpty(timestamp, nameof(timestamp));

            return DateTime.ParseExact(timestamp, _timestampFormats, CultureInfo.InvariantCulture).Ticks;
        }

        private static string BuildDescription(string description)
        {
            return SmartstoreVersion.CurrentFullVersion.Grow(description, " ");
        }
    }
}
