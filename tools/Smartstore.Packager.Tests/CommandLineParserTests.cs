#nullable enable

using NUnit.Framework;
using Smartstore.Packager.Cli;

namespace Smartstore.Packager.Tests;

[TestFixture]
public class CommandLineParserTests
{
    [Test]
    public void Can_parse_single_extension()
    {
        var result = CommandLineParser.Parse(["pack", "--root", @"C:\artifact", "--output", @"C:\out", "--extension", "Smartstore.PayPal"]);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.PackOptions!.RootPath, Is.EqualTo(@"C:\artifact"));
        Assert.That(result.PackOptions.OutputPath, Is.EqualTo(@"C:\out"));
        Assert.That(result.PackOptions.All, Is.False);
        Assert.That(result.PackOptions.ExtensionNames, Is.EqualTo(new[] { "Smartstore.PayPal" }));
    }

    [Test]
    public void Can_parse_repeated_extensions()
    {
        var result = CommandLineParser.Parse(
            ["pack", "--root", "r", "--output", "o", "--extension", "Smartstore.PayPal", "--extension", "Smartstore.Stripe"]);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.PackOptions!.ExtensionNames, Is.EqualTo(new[] { "Smartstore.PayPal", "Smartstore.Stripe" }));
    }

    [Test]
    public void Can_parse_all()
    {
        var result = CommandLineParser.Parse(["pack", "--root=r", "--output=o", "--all"]);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.PackOptions!.All, Is.True);
        Assert.That(result.PackOptions.ExtensionNames, Is.Empty);
    }

    [Test]
    public void All_and_extension_are_mutually_exclusive()
    {
        var result = CommandLineParser.Parse(["pack", "--root", "r", "--output", "o", "--all", "--extension", "Smartstore.PayPal"]);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Error, Does.Contain("mutually exclusive"));
    }

    [Test]
    public void Either_all_or_extension_is_required()
    {
        var result = CommandLineParser.Parse(["pack", "--root", "r", "--output", "o"]);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Error, Does.Contain("--all"));
    }

    [TestCase("--root")]
    [TestCase("--output")]
    public void Required_options_are_enforced(string missingOption)
    {
        string[] args = ["pack", "--root", "r", "--output", "o", "--all"];
        var reduced = new List<string>(args);
        var index = reduced.IndexOf(missingOption);
        reduced.RemoveRange(index, 2);

        var result = CommandLineParser.Parse(reduced);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Error, Does.Contain(missingOption));
    }

    [Test]
    public void Option_without_value_is_rejected()
    {
        var result = CommandLineParser.Parse(["pack", "--root", "--output", "o", "--all"]);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Error, Does.Contain("requires a value"));
    }

    [Test]
    public void Unknown_option_is_rejected()
    {
        var result = CommandLineParser.Parse(["pack", "--root", "r", "--output", "o", "--all", "--verbose"]);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Error, Does.Contain("--verbose"));
    }

    [Test]
    public void Unknown_command_is_rejected()
    {
        var result = CommandLineParser.Parse(["publish", "--all"]);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Error, Does.Contain("publish"));
    }

    [Test]
    public void Empty_command_line_is_rejected()
    {
        var result = CommandLineParser.Parse([]);

        Assert.That(result.IsValid, Is.False);
    }

    [TestCase("--help")]
    [TestCase("-h")]
    [TestCase("help")]
    public void Help_is_recognized(string arg)
    {
        var result = CommandLineParser.Parse([arg]);

        Assert.That(result.HelpRequested, Is.True);
        Assert.That(result.IsValid, Is.True);
    }
}
