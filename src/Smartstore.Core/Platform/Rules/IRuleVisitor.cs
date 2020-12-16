namespace Smartstore.Core.Rules
{
    public interface IRuleVisitor
    {
        RuleScope Scope { get; }
        IRuleExpression VisitRule(RuleEntity rule);
        IRuleExpressionGroup VisitRuleSet(RuleSetEntity ruleSet);
    }
}