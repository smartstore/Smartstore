# Rules Engine

## Overview

The rules engine lets administrators target entities such as discounts, categories, shipping or payment methods with flexible conditions. Rules are stored in **rule sets** that evaluate to *true* or *false* and are attached to entities implementing `IRulesContainer`.

## Key concepts

- **Rule set** – container that groups multiple rules rules with a logical operator (`AND` by default). Each rule set has a `RuleScope` so it only applies to matching providers (for example `Cart` or `Customer`).
- **Rule** – single condition composed of an operator and a value. Rules reference a descriptor that defines the data type and allowed operators.
- **Rule provider** – component that supplies rule descriptors for a specific scope and converts stored rule data into executable expressions.
- **Options provider** – delivers select list values (e.g. available customer roles) used by the rule editor when a descriptor requires lookup data.

## Attaching rule sets

Many domain entities, including discounts, categories and payment methods, implement `IRulesContainer`. In the admin UI you can pick one or more rule sets for these entities. At runtime the provider for the matching scope evaluates the rule sets to determine whether an entity is applicable.

## Creating a custom rule provider

```csharp
public class ZipCodeRuleProvider : RuleProviderBase, IMyZipCodeRuleProvider
{
    public ZipCodeRuleProvider() : base(MyRuleScopeId) { }

    protected override Task<IEnumerable<RuleDescriptor>> LoadDescriptorsAsync()
    {
        var descriptor = new MyRuleDescriptor
        {
            Name = "ZipCode",
            DisplayName = T("Admin.Rules.ZipCode"),
            RuleType = RuleType.String,
            ProcessorType = typeof(ZipCodeRule),
            Operators = [RuleOperator.IsEqualTo]
        };

        return Task.FromResult<IEnumerable<RuleDescriptor>>(new[] { descriptor });
    }

    public override IRuleExpressionGroup VisitRuleSet(RuleSetEntity ruleSet)
        => new RuleExpressionGroup { LogicalOperator = ruleSet.LogicalOperator };

    public override async Task<IRuleExpression> VisitRuleAsync(RuleEntity rule)
    {
        var expression = new RuleExpression();
        await ConvertRuleAsync(rule, expression);
        return expression;
    }
}
```

The provider advertises a single `ZipCode` descriptor. `ConvertRuleAsync` binds stored values to strongly typed expressions that the rules engine can evaluate.

## Evaluating rules

Use `IRuleService` to create an expression group and the matching provider to evaluate it:

```csharp
IRuleProvider provider = _ruleProviderFactory.GetProvider(MyRuleScopeId);
RuleExpression expression = await _ruleService.CreateExpressionGroupAsync(ruleSetId, _cartRuleProvider);
bool matches = await ((IMyZipCodeRuleProvider)provider).RuleMatchesAsync([expression], LogicalRuleOperator.And);
```

If `matches` is `true` the entity associated with the rule set match the rule conditions.
