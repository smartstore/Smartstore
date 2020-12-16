namespace Smartstore.Core.Rules.Rendering
{
    public interface IRuleTemplateSelector
    {
        /// <summary>
        /// Gets infos about a rule value template.
        /// </summary>
        /// <param name="descriptor">Rule descriptor.</param>
        /// <returns>Rule value template infos.</returns>
        RuleTemplateInfo GetTemplate(RuleDescriptor descriptor);
    }
}