using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Engine;

namespace Smartstore.Domain
{
    public abstract class EntityWithAttributes : BaseEntity
    {
        protected EntityWithAttributes()
        {
        }

        protected EntityWithAttributes(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets a specialized generic attributes collection for the current entity.
        /// Loaded data will be cached for the duration of the request.
        /// </summary>
        /// <returns>
        /// Generic attributes collection or <c>null</c> if <see cref="IGenericAttributeService"/> 
        /// is not registered in service container or entity is transient.
        /// </returns>
        [NotMapped, JsonIgnore]
        public virtual GenericAttributeCollection GenericAttributes
        {
            get
            {
                // INFO: Unfortuately covariant return type does not work when type is wrapped as Task<T>.
                // Therefore this method has to be sync.
                var service = EngineContext.Current.Scope.ResolveOptional<IGenericAttributeService>();
                if (service == null)
                {
                    return null;
                }

                return service.GetAttributesForEntity(GetEntityName(), Id);
            }
        }
    }
}
