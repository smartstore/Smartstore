using FluentMigrator;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2023-07-28 12:00:00", "Core: attribute combination hash code")]
    internal class AttributeCombinationHashCode : Migration, IDataSeeder<SmartDbContext>
    {
        const string AttributeCombinationTable = nameof(ProductVariantAttributeCombination);
        const string HashCodeColumn = nameof(ProductVariantAttributeCombination.HashCode);
        const string HashCodeIndex = "IX_HashCode";

        private readonly IProductAttributeService _productAttributeService;
        private readonly ILogger _logger;

        public AttributeCombinationHashCode(IProductAttributeService productAttributeService, ILogger logger)
        {
            _productAttributeService = productAttributeService;
            _logger = logger;
        }

        public override void Up()
        {
            if (!Schema.Table(AttributeCombinationTable).Column(HashCodeColumn).Exists())
            {
                Create.Column(HashCodeColumn).OnTable(AttributeCombinationTable)
                    .AsInt32()
                    .NotNullable()
                    .WithDefaultValue(0)
                    .Indexed(HashCodeIndex);
            }
        }

        public override void Down()
        {
            var attributeCombinations = Schema.Table(AttributeCombinationTable);

            if (attributeCombinations.Index(HashCodeIndex).Exists())
            {
                Delete.Index(HashCodeIndex).OnTable(AttributeCombinationTable);
            }

            if (attributeCombinations.Column(HashCodeColumn).Exists())
            {
                Delete.Column(HashCodeColumn).FromTable(AttributeCombinationTable);
            }
        }

        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            var numBatches = 0;
            var total = 0;
            int num;

            // Avoid an infinite loop here under all circumstances. Process a maximum of 500,000,000 records.
            do
            {
                num = await _productAttributeService.EnsureAttributeCombinationHashCodesAsync(5000, cancelToken);
                total += num;
            }
            while (num > 0 && ++numBatches < 100000 && !cancelToken.IsCancellationRequested);

            _logger.Debug($"Created hash codes for {total} product attribute combinations.");
        }
    }
}
