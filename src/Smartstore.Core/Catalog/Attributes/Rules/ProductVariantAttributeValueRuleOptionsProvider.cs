using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Catalog.Attributes.Rules
{
    public partial class ProductVariantAttributeValueRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;

        public ProductVariantAttributeValueRuleOptionsProvider(SmartDbContext db)
        {
            _db = db;
        }

        public int Order => 0;

        public bool Matches(string dataSource)
            => dataSource == KnownRuleOptionDataSourceNames.VariantValue;

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            if (context.DataSource != KnownRuleOptionDataSourceNames.VariantValue)
            {
                return null;
            }

            var metadata = context.Descriptor.Metadata;

            if (context.Reason == RuleOptionsRequestReason.SelectedDisplayNames)
            {
                var variants = await _db.ProductVariantAttributeValues.GetManyAsync(context.Value.ToIntArray());
                var options = variants.Select(x => new RuleValueSelectListOption
                {
                    Value = x.Id.ToString(),
                    Text = x.GetLocalized(y => y.Name, context.Language, true, false)
                });

                return RuleOptionsResult.Create(context, options);
            }
            else if (metadata.TryGetValue("ParentId", out var objParentId))
            {
                var pIndex = -1;
                var existingValues = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                var options = new List<RuleValueSelectListOption>();

                var tmpQuery = _db.ProductVariantAttributeValues
                    .AsNoTracking()
                    .Where(x => x.ProductVariantAttribute.ProductAttributeId == (int)objParentId);

                if (metadata.TryGetValueAs<bool>("AllowFiltering", out var allowFiltering))
                {
                    tmpQuery = tmpQuery.Where(x => x.ProductVariantAttribute.ProductAttribute.AllowFiltering == allowFiltering);
                }

                if (metadata.TryGetValueAs<ProductVariantAttributeValueType>("ValueType", out var valueType))
                {
                    tmpQuery = tmpQuery.Where(x => x.ValueTypeId == (int)valueType);
                }

                var query = tmpQuery.ApplyListTypeFilter();

                while (true)
                {
                    var variants = await query.ToPagedList(++pIndex, 1000).LoadAsync();
                    foreach (var variant in variants)
                    {
                        var name = variant.GetLocalized(x => x.Name, context.Language, true, false);
                        if (!existingValues.Contains(name))
                        {
                            existingValues.Add(name);
                            options.Add(new() { Value = variant.Id.ToString(), Text = name });
                        }
                    }
                    if (!variants.HasNextPage)
                    {
                        break;
                    }
                }

                return RuleOptionsResult.Create(context, options);
            }

            return RuleOptionsResult.Empty;
        }
    }
}
