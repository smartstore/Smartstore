using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Engine;

namespace Smartstore.Domain
{
    public abstract class EntityWithAttributes : BaseEntity
    {
        /// <summary>
        /// Gets a specialized generic attributes collection for the current entity.
        /// Loaded data will be cached for the duration of the request.
        /// </summary>
        /// <param name="storeId">
        /// If 0, current store id will be resolved automatically. Store-neutral
        /// attributes are always loaded.
        /// </param>
        /// <returns>Generic attributes collection</returns>
        public virtual GenericAttributeCollection GetAttributes(int storeId = 0)
        {
            // INFO: Unfortuately covariant return type does not work when type is wrapped as Task<T>.
            // Therefore this method has to be sync.
            var service = EngineContext.Current.Scope.ResolveOptional<IGenericAttributeService>();
            if (service == null)
            {
                return new GenericAttributeCollection(Enumerable.Empty<GenericAttribute>(), GetEntityName(), Id, storeId);
            }

            return service.GetAttributesForEntityAsync(Id, GetEntityName(), storeId).Await();
        }
    }
}
