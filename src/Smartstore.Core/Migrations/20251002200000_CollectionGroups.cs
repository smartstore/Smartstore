using System.Data;
using FluentMigrator;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2025-10-02 20:00:00", "Core: Collection groups")]
    internal class CollectionGroups : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        public override void Up()
        {
            const string groupTableName = nameof(CollectionGroup);
            const string mappingTableName = nameof(CollectionGroupMapping);
            const string specOptionTableName = nameof(SpecificationAttributeOption);
            const string specOptionColumnName = nameof(SpecificationAttributeOption.CollectionGroupMappingId);

            if (!Schema.Table(groupTableName).Exists())
            {
                Create.Table(groupTableName)
                    .WithIdColumn()
                    .WithColumn(nameof(CollectionGroup.EntityName)).AsString(100).NotNullable()
                        .Indexed()
                    .WithColumn(nameof(CollectionGroup.Name)).AsString(400).NotNullable()
                        .Indexed()
                    .WithColumn(nameof(CollectionGroup.Published)).AsBoolean().NotNullable()
                    .WithColumn(nameof(CollectionGroup.DisplayOrder)).AsInt32().NotNullable()
                        .Indexed();
            }

            if (!Schema.Table(mappingTableName).Exists())
            {
                Create.Table(mappingTableName)
                    .WithIdColumn()
                    .WithColumn(nameof(CollectionGroupMapping.CollectionGroupId)).AsInt32().NotNullable()
                        .Indexed()
                        .ForeignKey(groupTableName, nameof(BaseEntity.Id))
                        .OnDelete(Rule.Cascade)
                    .WithColumn(nameof(CollectionGroupMapping.EntityId)).AsInt32().NotNullable();
            }

            if (!Schema.Table(specOptionTableName).Column(specOptionColumnName).Exists())
            {
                Create.Column(specOptionColumnName).OnTable(specOptionTableName)
                    .AsInt32()
                    .Nullable()
                    .Indexed()
                    .ForeignKey(mappingTableName, nameof(BaseEntity.Id))
                    .OnDelete(Rule.SetNull);
            }
        }

        public override void Down()
        {
        }

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Permissions.DisplayName.CollectionGroup", "Collection Groups", "Gruppierungen");
            builder.AddOrUpdate("Admin.Configuration.CollectionGroups", "Collection Groups", "Gruppierungen");

            builder.AddOrUpdate("Admin.Catalog.Attributes.SpecificationAttributes.Options.Fields.CollectionGroup",
                "Collection Group",
                "Gruppierung",
                "Specifies an optional collection group. The option is then indented in the group.",
                "Legt eine optionale Gruppierung fest. Die Option wird dadurch in der Gruppe eingerückt dargestellt.");

            builder.AddOrUpdate("Admin.Configuration.CollectionGroup.Name",
                "Name",
                "Name",
                "Specifies the name of the collection group.",
                "Legt den Namen der Gruppierung fest.");

            builder.AddOrUpdate("Admin.Configuration.CollectionGroup.NumberOfAssignments",
                "Assignments",
                "Zuordnungen",
                "The number of objects assigned to the group.",
                "Die Anzahl der Objekte, die der Gruppierung zugeordnet sind.");
        }
    }
}
