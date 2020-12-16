namespace Smartstore.Core.Rules
{
    public interface IRuleConstraint
    {
        bool Match(RuleExpression expression);
    }
}
