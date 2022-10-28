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
        private Dictionary<string, object> _hookState;

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
        /// Adds custom state data to the entity that can be accessed in hook implementations.
        /// </summary>
        /// <remarks>The hook data bag is cleared after the entity was committed to the database.</remarks>
        /// <param name="state">Hook state data to add.</param>
        public void AddHookState(string key, object state)
        {
            Guard.NotEmpty(key, nameof(key));
            Guard.NotNull(state, nameof(state));
            
            _hookState ??= new(StringComparer.OrdinalIgnoreCase);
            _hookState[key] = state;
        }

        /// <summary>
        /// Gets hook state data for the given <paramref name="key"/>.
        /// </summary>
        public object GetHookState(string key)
        {
            Guard.NotEmpty(key, nameof(key));

            return _hookState?.Get(key);
        }

        /// <summary>
        /// Clears the hook data bag associated with this entity.
        /// </summary>
        public void ClearHookState()
        {
            _hookState?.Clear();
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
