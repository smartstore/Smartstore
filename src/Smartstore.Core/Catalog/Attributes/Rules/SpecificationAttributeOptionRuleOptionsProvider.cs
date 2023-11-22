using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Catalog.Attributes.Rules
{
    public partial class SpecificationAttributeOptionRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;

        public SpecificationAttributeOptionRuleOptionsProvider(SmartDbContext db)
        {
            _db = db;
        }

        public int Order => 0;

        public bool Matches(string dataSource)
            => dataSource == KnownRuleOptionDataSourceNames.AttributeOption;

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            if (context.DataSource != KnownRuleOptionDataSourceNames.AttributeOption)
            {
                return null;
            }

            if (context.Reason == RuleOptionsRequestReason.SelectedDisplayNames)
            {
                var attributes = await _db.SpecificationAttributeOptions.GetManyAsync(context.Value.ToIntArray());
                var options = attributes.Select(x => new RuleValueSelectListOption
                {
                    Value = x.Id.ToString(),
                    Text = x.GetLocalized(y => y.Name, context.Language, true, false)
                });

                return RuleOptionsResult.Create(context, options);
            }
            else if (context.Descriptor.Metadata.TryGetValue("ParentId", out var objParentId))
            {
                var attributes = await _db.SpecificationAttributeOptions
                    .AsNoTracking()
                    .Where(x => x.SpecificationAttributeId == (int)objParentId)
                    .OrderBy(x => x.DisplayOrder)
                    .ToPagedList(context.PageIndex, context.PageSize)
                    .LoadAsync();

                var options = attributes.AsQueryable().Select(x => new RuleValueSelectListOption
                {
                    Value = x.Id.ToString(),
                    Text = x.GetLocalized(y => y.Name, context.Language, true, false, false)
                });

                return RuleOptionsResult.Create(context, options, true, attributes.HasNextPage);
            }

            return RuleOptionsResult.Empty;
        }
    }
}
