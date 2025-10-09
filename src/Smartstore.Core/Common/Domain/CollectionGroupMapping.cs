using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Smartstore.Core.Common
{
    internal class CollectionGroupMappingMap : IEntityTypeConfiguration<CollectionGroupMapping>
    {
        public void Configure(EntityTypeBuilder<CollectionGroupMapping> builder)
        {
            builder.HasOne(c => c.CollectionGroup)
                .WithMany(c => c.CollectionGroupMappings)
                .HasForeignKey(c => c.CollectionGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public partial class CollectionGroupMapping : BaseEntity
    {
        public int CollectionGroupId { get; set; }

        private CollectionGroup _collectionGroup;
        public CollectionGroup CollectionGroup
        {
            get => _collectionGroup ?? LazyLoader.Load(this, ref _collectionGroup);
            set => _collectionGroup = value;
        }

        /// <summary>
        /// Gets or sets the Smartstore entity identifier.
        /// </summary>
        public int EntityId { get; set; }
    }
}
