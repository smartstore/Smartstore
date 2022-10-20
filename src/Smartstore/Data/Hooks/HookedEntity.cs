using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Smartstore.Domain;
using EfState = Microsoft.EntityFrameworkCore.EntityState;

namespace Smartstore.Data.Hooks
{
    public class HookedEntity : IHookedEntity
    {
        private Type _entityType;
        private bool? _isSoftDeleted;

        public HookedEntity(EntityEntry entry)
        {
            Entry = entry;
            InitialState = (EntityState)entry.State;
        }

        public DbContext DbContext
        {
            get => Entry.Context;
        }

        public EntityEntry Entry
        {
            get;
        }

        public BaseEntity Entity => Entry.Entity as BaseEntity;

        public Type EntityType => _entityType ??= Entry.Entity?.GetType();

        public EntityState InitialState
        {
            get;
            set;
        }

        public EntityState State
        {
            get => (EntityState)Entry.State;
            set => Entry.State = (EfState)((int)value);
        }

        public bool HasStateChanged => InitialState != State;

        public bool IsPropertyModified(string propertyName)
        {
            Guard.NotEmpty(propertyName, nameof(propertyName));

            if (State == EntityState.Modified)
            {
                var prop = Entry.Property(propertyName);
                if (prop == null)
                {
                    throw new InvalidOperationException($"An entity property '{propertyName}' does not exist.");
                }

                return prop.CurrentValue != null && !prop.CurrentValue.Equals(prop.OriginalValue);
            }

            return false;
        }

        public bool? IsSoftDeleted
        {
            get
            {
                if (_isSoftDeleted.HasValue)
                {
                    return _isSoftDeleted;
                }

                if (Entry.Entity is ISoftDeletable entity)
                {
                    return Entry.State == EfState.Modified
                        ? entity.Deleted && IsPropertyModified(nameof(ISoftDeletable.Deleted))
                        : entity.Deleted;
                }

                return null;
            }
            set
            {
                _isSoftDeleted = value;
            }
        }
    }
}