using System;
using System.Collections.Generic;
using NUnit.Framework;
using Smartstore.Core.Data.Migrations.Icons;
using Smartstore.IconKit;

namespace Smartstore.Core.Tests.Platform.Data.Migrations.Icons;

[TestFixture]
public class LegacyIconConverterTests
{
    private readonly LegacyIconConverter _converter = new();

    [TestCase(null)]
    [TestCase("")]
    [TestCase("solid")]
    [TestCase("regular")]
    public void Known_brands_override_missing_or_stale_styles(string style)
    {
        var converter = new LegacyIconConverter(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "github" });
        var result = converter.ConvertIcon("fa-github", style);
        Assert.That(result.Expression.Value.Address.Variant, Is.EqualTo("b"));
        Assert.That(converter.ConvertIcon("star", style).Expression.Value.Address.Variant,
            Is.EqualTo(style == "regular" ? "r" : "s"));
        Assert.That(converter.ConvertClasses("fa fa-github").Expression.Value.Address.Variant, Is.EqualTo("b"));
        Assert.That(converter.ConvertIcon("fa:github!/r").Expression.Value.Address.Variant, Is.EqualTo("r"));
    }

    [TestCase("fa-home", "fas", "s")]
    [TestCase("home", "regular", "r")]
    [TestCase("home", "fal", "l")]
    [TestCase("home", "duotone", "ds")]
    [TestCase("home", "fab", "b")]
    [TestCase("home", null, "s")]
    public void Legacy_values_produce_an_explicit_Font_Awesome_address(string name, string style, string variant)
    {
        var result = _converter.ConvertIcon(name, style, "primary");

        Assert.That(result.Expression.HasValue, Is.True);
        var expression = result.Expression.Value;
        Assert.That(expression.Address.Name, Is.EqualTo("home"));
        Assert.That(expression.Address.Library, Is.EqualTo("fa"));
        Assert.That(expression.Address.Variant, Is.EqualTo(variant));
        Assert.That(expression.Address.SkipMapping, Is.True);
        Assert.That(expression.Presentation.Color, Is.EqualTo("var(--primary)"));
        Assert.That(result.Warning, Is.Null);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void Empty_icon_values_have_nothing_to_convert(string name)
    {
        var result = _converter.ConvertIcon(name);
        Assert.That(result.Expression, Is.Null);
        Assert.That(result.Warning, Is.Null);
    }

    [Test]
    public void Invalid_theme_color_does_not_prevent_icon_conversion()
    {
        var result = _converter.ConvertIcon("fa-star", "far", "red;display:none");
        Assert.That(result.Expression.HasValue, Is.True);
        Assert.That(result.Expression.Value.Address.Name, Is.EqualTo("star"));
        Assert.That(result.Expression.Value.Presentation.Color, Is.Null);
        Assert.That(result.Warning, Is.Not.Null.And.Not.Empty);
    }

    [TestCase("fa:star!/r?rotate=90")]
    [TestCase("hi:cart!/sr?size=lg")]
    public void Qualified_expressions_are_not_reinterpreted(string value)
    {
        var result = _converter.ConvertIcon(value, "fas", "danger");
        Assert.That(result.Expression, Is.EqualTo(IconExpression.Parse(value)));
        Assert.That(result.Warning, Is.Null);
    }

    [TestCase("fas fa-home", "s")]
    [TestCase("fa-regular fa-home", "r")]
    [TestCase("fa-home fal", "l")]
    [TestCase("fa-duotone fa-home", "ds")]
    [TestCase("fab fa-home", "b")]
    [TestCase("fa-home", "s")]
    public void Classes_accept_short_and_long_styles_in_any_order(string classes, string variant)
    {
        var result = _converter.ConvertClasses(classes);
        Assert.That(result.Expression.HasValue, Is.True);
        Assert.That(result.Expression.Value.Address.Variant, Is.EqualTo(variant));
        Assert.That(result.Expression.Value.Address.Name, Is.EqualTo("home"));
        Assert.That(result.RemainingClasses, Is.Empty);
        Assert.That(result.Warning, Is.Null);
    }

    [Test]
    public void Modifiers_and_custom_classes_are_separated()
    {
        var result = _converter.ConvertClasses(
            "text-primary fas fa-star fa-fw fa-lg fa-rotate-90 fa-flip-horizontal fa-flip-vertical fa-spin fa-spin-reverse custom");

        Assert.That(result.Expression.HasValue, Is.True);
        var presentation = result.Expression.Value.Presentation;
        Assert.That(presentation.FixedWidth, Is.True);
        Assert.That(presentation.Size, Is.EqualTo("lg"));
        Assert.That(presentation.Rotate, Is.EqualTo(90));
        Assert.That(presentation.FlipHorizontal, Is.True);
        Assert.That(presentation.FlipVertical, Is.True);
        Assert.That(presentation.Animation, Is.EqualTo("spin"));
        Assert.That(presentation.AnimationReverse, Is.True);
        Assert.That(result.RemainingClasses, Is.EqualTo("text-primary custom"));
        Assert.That(result.Warning, Is.Null);
    }

    [TestCase("fas fa-home fa-star")]
    [TestCase("fas fa-home fa-stack-1x")]
    [TestCase("text-primary")]
    public void Unsupported_classes_return_a_warning_without_a_partial_expression(string classes)
    {
        var result = _converter.ConvertClasses(classes);
        Assert.That(result.Expression, Is.Null);
        Assert.That(result.RemainingClasses, Is.EqualTo(classes));
        Assert.That(result.Warning, Is.Not.Null.And.Not.Empty);
    }

    [TestCase("<i class=\"fas fa-home\"></i>")]
    [TestCase("<i class='fas fa-home'></i>")]
    [TestCase("<i class=\"fa-solid fa-home\">  </i>")]
    public void Html_converts_quoted_class_attributes(string html)
    {
        var result = _converter.ConvertHtml(html);
        Assert.That(result.Html, Is.EqualTo("<sm-icon name=\"home!\" lib=\"fa\" variant=\"s\"></sm-icon>"));
        Assert.That(result.Warnings, Is.Empty);
    }

    [Test]
    public void Html_preserves_surroundings_attributes_and_custom_classes()
    {
        var html = "<p>Before &amp; after</p><i id='icon' class='far fa-star text-primary' title='Favorite'></i>\r\n<footer>End</footer>";
        var result = _converter.ConvertHtml(html);

        Assert.That(result.Html, Is.EqualTo(
            "<p>Before &amp; after</p><sm-icon id='icon' title='Favorite' class=\"text-primary\" name=\"star!\" lib=\"fa\" variant=\"r\"></sm-icon>\r\n<footer>End</footer>"));
        Assert.That(result.Warnings, Is.Empty);
        Assert.That(_converter.ConvertHtml(result.Html).Html, Is.EqualTo(result.Html));
    }

    [Test]
    public void Html_emits_separate_attributes_and_decodes_class_entities()
    {
        var result = _converter.ConvertHtml("<i class=\"fas&#32;fa-star fa-lg fa-spin fa-fw fa-rotate-90 fa-flip-horizontal fa-inverse fa-spin-reverse\"></i>");

        Assert.That(result.Html, Is.EqualTo(
            "<sm-icon name=\"star!\" lib=\"fa\" variant=\"s\" size=\"lg\" fw=\"true\" inverse=\"true\" rotate=\"90\" flip-h=\"true\" animation=\"spin\" animation-reverse=\"true\"></sm-icon>"));
        Assert.That(result.Warnings, Is.Empty);
    }

    [TestCase("<i></i><span class=\"fas fa-home\"></span>")]
    [TestCase("<i class=\"other\"></i>")]
    [TestCase("<i class=\"fas fa-home\">Keep this text</i>")]
    [TestCase("<sm-icon name=\"home!\" lib=\"fa\" variant=\"s\"></sm-icon>")]
    public void Html_leaves_nonmatching_content_unchanged(string html)
    {
        var result = _converter.ConvertHtml(html);
        Assert.That(result.Html, Is.SameAs(html));
        Assert.That(result.Warnings, Is.Empty);
        Assert.That(result.ConvertedIcons, Is.Zero);
        Assert.That(result.NotConvertedIcons, Is.Zero);
    }

    [Test]
    public void Html_continues_after_an_unsupported_icon()
    {
        var unsupported = "<i class='fas fa-home fa-stack-1x'></i>";
        var result = _converter.ConvertHtml(unsupported + "<i class='far fa-star'></i><i class='fas fa-home'></i>");
        Assert.That(result.Html, Is.EqualTo(unsupported +
            "<sm-icon name=\"star!\" lib=\"fa\" variant=\"r\"></sm-icon><sm-icon name=\"home!\" lib=\"fa\" variant=\"s\"></sm-icon>"));
        Assert.That(result.Warnings, Has.Count.EqualTo(1));
        Assert.That(result.ConvertedIcons, Is.EqualTo(2));
        Assert.That(result.NotConvertedIcons, Is.EqualTo(1));
    }

    [Test]
    public void Long_html_preserves_large_sections_between_matches()
    {
        var section = new string('x', 100_000);
        var html = section + "<i class='fas fa-home'></i>" + section + "<i class='far fa-star'></i>" + section;
        var result = _converter.ConvertHtml(html);
        Assert.That(result.Html, Is.EqualTo(section + "<sm-icon name=\"home!\" lib=\"fa\" variant=\"s\"></sm-icon>" +
            section + "<sm-icon name=\"star!\" lib=\"fa\" variant=\"r\"></sm-icon>" + section));
        Assert.That(result.Warnings, Is.Empty);
    }
}
