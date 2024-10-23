using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Search;

namespace Smartstore.Core.Catalog.Search
{
    /// <summary>
    /// Helper for building catalog search query including <see cref="LocalizedProperty"/> using <see cref="MemberExpression"/>.
    /// </summary>
    internal class TermSearchProduct
    {
        public Product Product { get; set; }
        public LocalizedProperty Translation { get; set; }

        public static FilterExpression CreateFilter(
            Expression<Func<TermSearchProduct, string>> productExpression,
            Expression<Func<TermSearchProduct, string>> translationExpression,
            IAttributeSearchFilter filter,
            int languageId = 0, 
            bool parseTerm = false)
        {
            Guard.NotNull(productExpression);
            Guard.NotNull(translationExpression);
            Guard.NotNull(filter);

            var pFilter = CreateExpression(productExpression, filter, RuleScope.Product, parseTerm);

            if (languageId == 0)
            {
                // Ignore localized property.
                return pFilter;
            }

            var tFilter = CreateExpression(translationExpression, filter, RuleScope.Other, parseTerm);
            var propertyName = ((MemberExpression)productExpression.Body).Member.Name;

            var lpFilters = new FilterExpression[]
            {
                new()
                {
                    Descriptor = new FilterDescriptor<TermSearchProduct, int>(x => x.Translation.LanguageId, RuleScope.Other),
                    Operator = RuleOperator.IsEqualTo,
                    Value = languageId
                },
                new()
                {
                    Descriptor = new FilterDescriptor<TermSearchProduct, string>(x => x.Translation.LocaleKeyGroup, RuleScope.Other),
                    Operator = RuleOperator.IsEqualTo,
                    Value = nameof(Product)
                },
                new()
                {
                    Descriptor = new FilterDescriptor<TermSearchProduct, string>(x => x.Translation.LocaleKey, RuleScope.Other),
                    Operator = RuleOperator.IsEqualTo,
                    Value = propertyName
                },
                tFilter
            };

            // p.Name.StartsWith(term) || (lp.LanguageId == languageId && lp.LocaleKeyGroup == "Product" && lp.LocaleKey == "Name" && lp.LocaleValue.StartsWith(term))
            var expressions = new FilterExpression[]
            {
                pFilter,
                new FilterExpressionGroup(typeof(TermSearchProduct), lpFilters)
                {
                    LogicalOperator = LogicalRuleOperator.And
                }
            };

            return new FilterExpressionGroup(typeof(TermSearchProduct), expressions)
            {
                LogicalOperator = LogicalRuleOperator.Or
            };
        }

        private static FilterExpression CreateExpression(
            Expression<Func<TermSearchProduct, string>> memberExpression,
            IAttributeSearchFilter filter,
            RuleScope scope, 
            bool parseTerm)
        {
            if (parseTerm && FilterExpressionParser.TryParse(memberExpression, filter.Term?.ToString(), out var result))
            {
                return result;
            }

            return new()
            {
                Descriptor = new FilterDescriptor<TermSearchProduct, string>(memberExpression, scope),
                Operator = filter.GetOperator(),
                Value = filter.Term
            };
        }
    }
}
