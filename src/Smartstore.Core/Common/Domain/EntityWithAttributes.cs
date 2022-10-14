using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;

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
        /// is not registered in service container. If entity is transient, a readonly collection
        /// is returned.
        /// </returns>
        [NotMapped, IgnoreDataMember]
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
