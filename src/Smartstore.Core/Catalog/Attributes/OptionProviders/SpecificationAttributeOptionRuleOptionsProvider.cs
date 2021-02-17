using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Catalog.Attributes
{
    public partial class SpecificationAttributeOptionRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;

        public SpecificationAttributeOptionRuleOptionsProvider(SmartDbContext db)
        {
            _db = db;
        }

        public int Ordinal => 0;

        public bool Matches(string dataSource)
        {
            return dataSource == KnownRuleOptionDataSourceNames.AttributeOption;
        }

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            var result = new RuleOptionsResult();

            if (context.DataSource == KnownRuleOptionDataSourceNames.AttributeOption)
            {
                if (context.Reason == RuleOptionsRequestReason.SelectedDisplayNames)
                {
                    var attributes = await _db.SpecificationAttributeOptions.GetManyAsync(context.Value.ToIntArray());

                    result.AddOptions(context, attributes.Select(x => new RuleValueSelectListOption
                    {
                        Value = x.Id.ToString(),
                        Text = x.GetLocalized(y => y.Name, context.Language, true, false)
                    }));
                }
                else if (context.Descriptor.Metadata.TryGetValue("ParentId", out var objParentId))
                {
                    var attributes = await _db.SpecificationAttributeOptions
                        .AsNoTracking()
                        .Where(x => x.SpecificationAttributeId == (int)objParentId)
                        .OrderBy(x => x.DisplayOrder)
                        .ToPagedList(context.PageIndex, context.PageSize)
                        .LoadAsync();

                    result.IsPaged = true;
                    result.HasMoreData = attributes.HasNextPage;

                    result.AddOptions(context, attributes.AsQueryable().Select(x => new RuleValueSelectListOption
                    {
                        Value = x.Id.ToString(),
                        Text = x.GetLocalized(y => y.Name, context.Language, true, false, false)
                    }));
                }
            }
            else
            {
                return null;
            }

            return result;
        }
    }
}
