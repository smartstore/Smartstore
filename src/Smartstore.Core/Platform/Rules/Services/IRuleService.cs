using System.Threading.Tasks;
using Smartstore.Domain;

namespace Smartstore.Core.Rules
{
    /// <summary>
    /// Rule service interface.
    /// </summary>
    public partial interface IRuleService
    {
        // TODO: (mg) (core) Port rule classes from SmartStore.Services.Rules > Smartstore.Core.Rules
        // TODO: (mg) (core) Port rule classes from SmartStore.Services.Cart.Rules > Smartstore.Core.Checkout.Rules
        // TODO: (mg) (core) Port rule classes from SmartStore.Services.Catalog.Rules > Smartstore.Core.Catalog.Rules
        // TODO: (mg) (core) Port rule classes from SmartStore.Services.Customers[.Rules] > Smartstore.Core.Customers.Rules

        /// <summary>
        /// Applies given <paramref name="selectedRuleSetIds"/> to <paramref name="entity"/>.
        /// The caller is responsible for db commit.
        /// </summary>
        /// <param name="entity">The entity to apply rulesets to.</param>
        /// <param name="selectedRuleSetIds">Rulesets to apply.</param>
        /// <returns><c>true</c> if a database commit is required. <c>false</c> if nothing changed.</returns>
        Task<bool> ApplyRuleSetMappingsAsync<T>(T entity, int[] selectedRuleSetIds) where T : BaseEntity, IRulesContainer;

        Task<IRuleExpressionGroup> CreateExpressionGroupAsync(int ruleSetId, IRuleVisitor visitor, bool includeHidden = false);
        Task<IRuleExpressionGroup> CreateExpressionGroupAsync(RuleSetEntity ruleSet, IRuleVisitor visitor, bool includeHidden = false);
    }
}