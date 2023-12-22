namespace Smartstore.Core.Rules
{
    public enum RuleScope
    {
        Cart = 0,
        //OrderItem = 1,
        Customer = 2,
        Product = 3,
        ProductAttribute = 20,
        Other = int.MaxValue
    }
}
