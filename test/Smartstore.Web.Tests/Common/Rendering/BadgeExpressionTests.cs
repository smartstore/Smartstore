using NUnit.Framework;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Tests.Common.Rendering;

[TestFixture]
public class BadgeExpressionTests
{
    [TestCase(null, "badge badge-secondary")]
    [TestCase("", "badge badge-secondary")]
    [TestCase("PILL primary subtle", "badge badge-primary badge-subtle badge-pill")]
    [TestCase("danger outline ring pill", "badge badge-danger badge-outline badge-ring badge-pill")]
    [TestCase("subtle outline primary", "badge badge-primary badge-outline")]
    [TestCase("primary pill pill unknown", "badge badge-primary badge-pill")]
    public void Converts_expression_to_known_classes(string expression, string expected)
    {
        Assert.That(BadgeExpression.ToCssClasses(expression), Is.EqualTo(expected));
    }
}
