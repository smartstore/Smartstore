namespace Smartstore.Data.Caching
{
    /// <summary>
    /// Cached Data
    /// </summary>
    public class EFCacheData
    {
        /// <summary>
        /// DbDataReader's result.
        /// </summary>
        public DbTableRows TableRows { get; set; }

        /// <summary>
        /// DbDataReader's NonQuery result.
        /// </summary>
        public int NonQuery { get; set; }

        /// <summary>
        /// DbDataReader's Scalar result.
        /// </summary>
        public object Scalar { get; set; }

        /// <summary>
        /// Is result of the query null?
        /// </summary>
        public bool IsNull { get; set; }
    }

    public class EfRequestCacheData : EFCacheData
    {
        public object Result { get; set; }
    }
}