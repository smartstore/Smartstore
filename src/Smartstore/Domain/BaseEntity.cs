using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Smartstore.Domain
{
    /// <summary>
    /// Base class for entities
    /// </summary>
    public abstract partial class BaseEntity : INamedEntity, IEquatable<BaseEntity>
    {
        protected BaseEntity()
        {
        }

        protected BaseEntity(ILazyLoader lazyLoader)
        {
            LazyLoader = lazyLoader;
        }

        [NotMapped] // TODO: (core) Remove [NotMappedAttribute] once the EF bug (https://github.com/dotnet/efcore/issues/23968) is fixed.
        protected internal virtual ILazyLoader LazyLoader { get; set; }

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

        public override bool Equals(object obj)
        {
            return this.Equals(obj as BaseEntity);
        }

        bool IEquatable<BaseEntity>.Equals(BaseEntity other)
        {
            return this.Equals(other);
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
            return !this.IsTransientRecord() && !other.IsTransientRecord() && this.Id == other.Id;
        }
    }
}
