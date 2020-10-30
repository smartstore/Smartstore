namespace Smartstore.Data.Caching
{
    /// <summary>
    /// TableRow's structure
    /// </summary>
    public class DbTableRow
    {
        /// <summary>
        /// An array of objects with the column values of the current row.
        /// </summary>
        public object[] Values { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates the depth of nesting for the current row.
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Gets the number of columns in the current row.
        /// </summary>
        public int FieldCount => Values.Length;

        /// <summary>
        /// Returns Values[ordinal]
        /// </summary>
        public object this[int ordinal] => Values[ordinal];
    }
}