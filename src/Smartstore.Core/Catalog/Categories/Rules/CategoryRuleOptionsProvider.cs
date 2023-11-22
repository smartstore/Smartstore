using Smartstore.Core.Data;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Rendering;

namespace Smartstore.Core.Catalog.Categories.Rules
{
    public partial class CategoryRuleOptionsProvider : IRuleOptionsProvider
    {
        private readonly SmartDbContext _db;
        private readonly ICategoryService _categoryService;

        public CategoryRuleOptionsProvider(SmartDbContext db, ICategoryService categoryService)
        {
            _db = db;
            _categoryService = categoryService;
        }

        public int Order => 0;

        public bool Matches(string dataSource)
            => dataSource == KnownRuleOptionDataSourceNames.Category;

        public async Task<RuleOptionsResult> GetOptionsAsync(RuleOptionsContext context)
        {
            if (context.DataSource != KnownRuleOptionDataSourceNames.Category)
            {
                return null;
            }

            if (context.Reason == RuleOptionsRequestReason.SelectedDisplayNames)
            {
                var categories = await _db.Categories.GetManyAsync(context.Value.ToIntArray());
                var options = await categories
                    .SelectAwait(async x => new RuleValueSelectListOption
                    {
                        Value = x.Id.ToString(),
                        Text = (await _categoryService.GetCategoryPathAsync(x, context.Language.Id)).NullEmpty() ?? x.Name
                    })
                    .ToListAsync();

                return RuleOptionsResult.Create(context, options);
            }
            else
            {
                var categories = await _categoryService.GetCategoryTreeAsync(0, true);
                var options = await categories
                    .Flatten(false)
                    .SelectAwait(async x => new RuleValueSelectListOption
                    {
                        Value = x.Id.ToString(),
                        Text = (await _categoryService.GetCategoryPathAsync(x, context.Language.Id)).NullEmpty() ?? x.Name
                    })
                    .ToListAsync();

                return RuleOptionsResult.Create(context, options);
            }
        }
    }
}
