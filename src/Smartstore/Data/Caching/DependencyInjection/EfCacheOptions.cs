using System;
using System.Collections.Generic;
using Smartstore.Domain;

namespace Microsoft.Extensions.DependencyInjection
{
    public class CacheableEntity
    {
        public Type ClrType { get; internal set; }
        public TimeSpan? Timeout { get; internal set; }
        public int? MaxRows { get; internal set; }
    }
    
    public class EfCacheOptions
    {
        /// <summary>
        /// Should the debug level logging be disabled?
        /// </summary>
        public bool DisableLogging { set; get; }

        public IDictionary<Type, CacheableEntity> CacheableEntities { get; } = new Dictionary<Type, CacheableEntity>();

        public void AddCacheableEntity<T>(TimeSpan? timeout, int? maxRows)
            where T : BaseEntity, new()
        {

        }
    }
}
