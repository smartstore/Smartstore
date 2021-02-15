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

        /// <summary>
        /// Creates an expression group for a ruleset.
        /// </summary>
        /// <param name="ruleSetId">Ruleset identifier.</param>
        /// <param name="visitor">Rule visitor.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden rulesets.</param>
        /// <returns>Expression group.</returns>
        Task<IRuleExpressionGroup> CreateExpressionGroupAsync(int ruleSetId, IRuleVisitor visitor, bool includeHidden = false);

        /// <summary>
        /// Creates an expression group for a ruleset.
        /// </summary>
        /// <param name="ruleSet">Ruleset.</param>
        /// <param name="visitor">Rule visitor.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden rulesets.</param>
        /// <returns>Expression group.</returns>
        Task<IRuleExpressionGroup> CreateExpressionGroupAsync(RuleSetEntity ruleSet, IRuleVisitor visitor, bool includeHidden = false);
    }
}