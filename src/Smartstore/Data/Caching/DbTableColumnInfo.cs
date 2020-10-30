using System;

namespace Smartstore.Data.Caching
{
    /// <summary>
    /// TableColumn's Info
    /// </summary>
    public class DbTableColumnInfo
    {
        /// <summary>
        /// The column's ordinal.
        /// </summary>
        public int Ordinal { get; set; }

        /// <summary>
        /// The column's name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The column's DbType Name.
        /// </summary>
        public string DbTypeName { get; set; }

        /// <summary>
        /// The column's Type.
        /// </summary>
        public string TypeName { get; set; }
    }
}