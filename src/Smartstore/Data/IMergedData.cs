namespace Smartstore.Data
{
    /// <summary>
    /// Represents the possibility of data merging of two objects.
    /// </summary>
    public interface IMergedData
    {
        /// <summary>
        /// Gets or sets a value indicating whether to data should be merged with those of the related entity.
        /// </summary>
        bool MergedDataIgnore { get; set; }

        /// <summary>
        /// Gets the dictionary with the data to be merged.
        /// </summary>
        Dictionary<string, object> MergedDataValues { get; }
    }

    public static class IMergedDataExtensions
    {
        public static T GetMergedDataValue<T>(this IMergedData mergedData, string key, T defaultValue)
        {
            if (mergedData.MergedDataValues == null)
                return defaultValue;

            if (mergedData.MergedDataIgnore)
                return defaultValue;

            // TODO: (core) Refactor IMergedDataExtensions.GetMergedDataValue<T>()
            //if (mergedData is BaseEntity && HostingEnvironment.IsHosted)
            //{
            //    // This is absolutely bad coding! But I don't see any alternatives.
            //    // When the passed object is a (EF)-trackable entity,
            //    // we cannot return the merged value while EF performs
            //    // change detection, because entity properties could be set to modified,
            //    // where in fact nothing has changed.
            //    var dbContext = EngineContext.Current.Resolve<IDbContext>();
            //    if (dbContext.IsDetectingChanges())
            //    {
            //        return defaultValue;
            //    }
            //}

            if (mergedData.MergedDataValues.TryGetValue(key, out var value))
            {
                return (T)value;
            }

            return defaultValue;
        }
    }
}
