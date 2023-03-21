namespace Smartstore.Data.Providers
{
    /// <summary>
    /// Represents information about a database table.
    /// </summary>
    public class DbTableInfo
    {
        /// <summary>
        /// The table name.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// The number of rows in the table.
        /// </summary>
        public long NumRows { get; set; }

        /// <summary>
        /// The total space used by the table, in bytes.
        /// </summary>
        public long TotalSpace { get; set; }

        /// <summary>
        /// The amount of space used by the table, in bytes.
        /// </summary>
        public long UsedSpace { get; set; }
    }
}
