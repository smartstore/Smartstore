# Rules Engine

## Overview

The rules engine lets administrators target entities such as discounts, categories, shipping or payment methods with flexible conditions. Rules are stored in **rule sets** that evaluate to *true* or *false* and are attached to entities implementing `IRulesContainer`.

## Key concepts

- **Rule set** – container that groups multiple rules with a logical operator (`AND`/`OR`). Each rule set has a `RuleScope` so it only applies to matching providers (for example `Cart` or `Customer`).
- **Rule** – single condition composed of an operator and a value. Rules reference a descriptor that defines the data type and allowed operators.
- **Rule provider** – component that supplies rule descriptors for a specific scope and converts stored rule data into executable expressions.
- **Options provider** – delivers select list values (e.g., available customer roles) used by the rule editor when a descriptor requires lookup data.

## Built‑in rule providers

Smartstore ships with several `IRuleProvider` implementations, each tied to a `RuleScope`:

- **`CartRuleProvider`** (`RuleScope.Cart`) evaluates cart and checkout conditions for discounts, shipping rates and payment methods.
- **`ProductRuleProvider`** (`RuleScope.Product`) exposes product‑related descriptors that drive catalog filtering and search.
- **`TargetGroupService`** (`RuleScope.Customer`) creates a customer query based on `FilterDescriptor`s to assign customers to customer roles.
- **`AttributeRuleProvider`** (`RuleScope.ProductAttribute`) checks which product attributes are active for a current selection of product attributes.

Modules may register additional providers for custom scopes.

## Attaching rule sets

Many domain entities, including discounts, categories and payment methods, implement `IRulesContainer`. In the admin UI you can pick one or more rule sets for these entities. At runtime the provider for the matching scope evaluates the rule sets to determine whether an entity is applicable.

## Creating a custom rule provider

Start by implementing a rule that inspects the cart context:

```csharp
using Smartstore.Core.Checkout.Rules;
using Smartstore.Core.Rules;

internal class ZipCodeRule : IRule<CartRuleContext>
{
    public Task<bool> MatchAsync(CartRuleContext context, RuleExpression expression)
    {
        var zip = context.Customer?.ShippingAddress?.ZipPostalCode;
        var match = zip != null && expression.HasListMatch(zip, StringComparer.InvariantCultureIgnoreCase);
        return Task.FromResult(match);
    }
}
```

Then wire it up in a provider:

```csharp
// Use an existing scope such as RuleScope.Cart or define your own value.
const RuleScope MyRuleScopeId = RuleScope.Cart; // see RuleScope.* for built‑in identifiers

public class ZipCodeRuleProvider : RuleProviderBase
{
    public ZipCodeRuleProvider() : base(MyRuleScopeId) { }

    protected override Task<IEnumerable<RuleDescriptor>> LoadDescriptorsAsync()
    {
        var descriptor = new CartRuleDescriptor
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
    {
        // Called once per rule set. Create the container for the rule expressions
        // and copy the logical operator (AND/OR) from the persisted entity.
        return new RuleExpressionGroup { LogicalOperator = ruleSet.LogicalOperator };
    }

    public override async Task<IRuleExpression> VisitRuleAsync(RuleEntity rule)
    {
        // Called for each rule in the set. ConvertRuleAsync binds the descriptor,
        // operator and value to a strongly typed RuleExpression that can be evaluated.
        var expression = new RuleExpression();
        await ConvertRuleAsync(rule, expression);
        return expression;
    }
}
```

The provider advertises a single `ZipCode` descriptor. `VisitRuleSet` and `VisitRuleAsync` are hooks that translate stored rule
entities into executable expressions; overriding them is required for custom logic, though the implementation is often just the
minimal wiring shown above. `ConvertRuleAsync` binds stored values to strongly typed expressions that the rules engine can evaluate.

## Evaluating rules

Use `IRuleService` to create an expression group and the matching provider to evaluate it. `ruleSetId` is the identifier of a
rule set attached to an entity implementing `IRulesContainer` (for example `discount.RuleSets[0].Id`). This code would typically
run inside a service that needs to check applicability, such as a shipping-rate provider or discount service:

```csharp
var ruleSetId = entity.RuleSets.First(x => x.Scope == RuleScope.Cart).Id; // Rule set assigned in admin UI
var group = await _ruleService.CreateExpressionGroupAsync(ruleSetId, _cartRuleProvider);
var matches = await _cartRuleProvider.RuleMatchesAsync(new[] { group }, LogicalRuleOperator.And);

if (matches)
{
    // The entity applies in the current context
}
```

`RuleMatchesAsync` returns `true` when all evaluated rule sets match the provided context.