using System;
using Smartstore;

namespace FluentValidation
{
    public static class FluentValidatorRuleExtensions
    {
        public static IRuleBuilderOptions<T, string> CreditCardCvvNumber<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.Matches(RegularExpressions.IsCvv);
        }
    }
}
