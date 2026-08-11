using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;

namespace Smartstore.Domain;

public abstract class EntityWithAttributes : BaseEntity
{
    private GenericAttributeCollection _genericAttributes;

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
            if (_genericAttributes != null)
            {
                return _genericAttributes;
            }

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

    /// <summary>
    /// Overrides the generic attribute collection resolved for this entity instance.
    /// </summary>
    /// <param name="attributes">The collection to return from <see cref="GenericAttributes"/>.</param>
    internal void SetGenericAttributes(GenericAttributeCollection attributes)
    {
        _genericAttributes = Guard.NotNull(attributes);
    }
}
