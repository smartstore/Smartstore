using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Data;

namespace Smartstore.Domain
{
    /// <summary>
    /// Base class for entities
    /// </summary>
    public abstract partial class BaseEntity : INamedEntity, IEquatable<BaseEntity>
    {
        private ILazyLoader _lazyLoader;
        private List<object> _hookData;

        protected BaseEntity()
        {
        }

        protected BaseEntity(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        [IgnoreDataMember]
        protected internal virtual ILazyLoader LazyLoader
        {
            get => _lazyLoader ?? NullLazyLoader.Instance;
            set => _lazyLoader = value;
        }

        /// <summary>
        /// Gets or sets the entity identifier
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual string GetEntityName()
        {
            return GetType().Name;
        }

        /// <summary>
        /// Transient objects are not associated with an item already in storage. For instance,
        /// a Product entity is transient if its Id is 0.
        /// </summary>
        public virtual bool IsTransientRecord()
        {
            return Id == 0;
        }

        /// <summary>
        /// Adds custom data to the entity that can be accessed in hook implementations.
        /// </summary>
        /// <remarks>The hook data bag is cleared after the entity was committed to the database.</remarks>
        /// <param name="data">Hook data to add.</param>
        public void AddHookData(object data)
        {
            Guard.NotNull(data, nameof(data));
            
            _hookData ??= new();
            _hookData.Add(data);
        }

        /// <summary>
        /// Gets hook data of type <typeparamref name="T"/> associated with this entity.
        /// </summary>
        public IEnumerable<T> GetHookData<T>()
        {
            return _hookData?.OfType<T>() ?? Enumerable.Empty<T>();
        }

        /// <summary>
        /// Clears the hook data bag associated with this entity.
        /// </summary>
        public void ClearHookData()
        {
            _hookData?.Clear();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BaseEntity);
        }

        bool IEquatable<BaseEntity>.Equals(BaseEntity other)
        {
            return Equals(other);
        }

        protected virtual bool Equals(BaseEntity other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (HasSameNonDefaultIds(other))
            {
                var otherType = other.GetType();
                var thisType = GetType();
                return thisType.Equals(otherType);
            }

            return false;
        }

        public override int GetHashCode()
        {
            if (IsTransientRecord())
            {
                return base.GetHashCode();
            }
            else
            {
                unchecked
                {
                    // It's possible for two objects to return the same hash code based on
                    // identically valued properties, even if they're of two different types,
                    // so we include the object's type in the hash calculation
                    var hashCode = GetType().GetHashCode();
                    return (hashCode * 31) ^ Id.GetHashCode();
                }
            }
        }

        public static bool operator ==(BaseEntity x, BaseEntity y)
        {
            return Equals(x, y);
        }

        public static bool operator !=(BaseEntity x, BaseEntity y)
        {
            return !Equals(x, y);
        }

        private bool HasSameNonDefaultIds(BaseEntity other)
        {
            return !IsTransientRecord() && !other.IsTransientRecord() && Id == other.Id;
        }
    }
}
