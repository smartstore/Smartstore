#nullable enable

using NUnit.Framework;
using Smartstore.Packager.Cli;

namespace Smartstore.Packager.Tests;

[TestFixture]
public class PackagerCliApplicationTests
{
    private StringWriter _out = default!;
    private StringWriter _error = default!;

    [SetUp]
    public void SetUp()
    {
        _out = new StringWriter();
        _error = new StringWriter();
    }

    [TearDown]
    public void TearDown()
    {
        _out.Dispose();
        _error.Dispose();
    }

    [Test]
    public async Task Help_prints_usage_and_returns_success()
    {
        var exitCode = await RunAsync("--help");

        Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        Assert.That(_out.ToString(), Does.Contain("Usage:"));
        Assert.That(_error.ToString(), Is.Empty);
    }

    [Test]
    public async Task Invalid_command_line_returns_invalid_command_line_code()
    {
        var exitCode = await RunAsync("pack", "--root", "r", "--output", "o");

        Assert.That(exitCode, Is.EqualTo(ExitCodes.InvalidCommandLine));
        Assert.That(_error.ToString(), Does.Contain("Usage:"));
        Assert.That(_out.ToString(), Is.Empty);
    }

    [Test]
    public async Task Missing_root_directory_returns_error_code()
    {
        var missing = Path.Combine(Path.GetTempPath(), "Smartstore.Packager.Tests", Guid.NewGuid().ToString("N"));

        var exitCode = await RunAsync("pack", "--root", missing, "--output", missing, "--all");

        Assert.That(exitCode, Is.EqualTo(ExitCodes.Error));
        Assert.That(_error.ToString(), Does.Contain("does not exist"));
    }

    [Test]
    public async Task Unknown_extension_returns_error_code()
    {
        using var artifact = new TempArtifactRoot();
        artifact.AddModule("Modules/Smartstore.PayPal", "Smartstore.PayPal");

        var exitCode = await RunAsync(
            "pack", "--root", artifact.Root, "--output", artifact.OutputPath, "--extension", "Smartstore.DoesNotExist");

        Assert.That(exitCode, Is.EqualTo(ExitCodes.Error));
        Assert.That(_error.ToString(), Does.Contain("Unknown extension 'Smartstore.DoesNotExist'"));
    }

    [Test]
    public async Task Packs_known_extension_case_insensitively()
    {
        using var artifact = new TempArtifactRoot();
        artifact.AddModule("Modules/Smartstore.PayPal", "Smartstore.PayPal");

        var exitCode = await RunAsync(
            "pack", "--root", artifact.Root, "--output", artifact.OutputPath, "--extension", "smartstore.paypal");

        var expected = Path.Combine(artifact.OutputPath, "Smartstore.Module.Smartstore.PayPal.6.0.0.zip");

        Assert.That(_error.ToString(), Is.Empty);
        Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));
        Assert.That(_out.ToString().Trim(), Is.EqualTo(expected));
        Assert.That(File.Exists(expected), Is.True);
        Assert.That(new FileInfo(expected).Length, Is.GreaterThan(0));
    }

    [Test]
    public async Task Packs_all_modules_and_themes()
    {
        using var artifact = new TempArtifactRoot();
        artifact.AddModule("Modules/Smartstore.PayPal", "Smartstore.PayPal");
        artifact.AddModule("Modules/Smartstore.Stripe", "Smartstore.Stripe");
        artifact.AddTheme("Themes/Flex", "Flex");

        var exitCode = await RunAsync("pack", "--root", artifact.Root, "--output", artifact.OutputPath, "--all");

        Assert.That(_error.ToString(), Is.Empty);
        Assert.That(exitCode, Is.EqualTo(ExitCodes.Success));

        var files = Directory.GetFiles(artifact.OutputPath, "*.zip").Select(Path.GetFileName).Order().ToArray();

        Assert.That(files, Is.EqualTo(new[]
        {
            "Smartstore.Module.Smartstore.PayPal.6.0.0.zip",
            "Smartstore.Module.Smartstore.Stripe.6.0.0.zip",
            "Smartstore.Theme.Flex.6.0.0.zip"
        }));
    }

    [Test]
    public async Task Continues_after_a_failure_and_returns_error_code()
    {
        using var artifact = new TempArtifactRoot();
        artifact.AddModule("Modules/Smartstore.PayPal", "Smartstore.PayPal");

        var exitCode = await RunAsync(
            "pack",
            "--root", artifact.Root,
            "--output", artifact.OutputPath,
            "--extension", "Smartstore.DoesNotExist",
            "--extension", "Smartstore.PayPal");

        Assert.That(exitCode, Is.EqualTo(ExitCodes.Error));
        Assert.That(File.Exists(Path.Combine(artifact.OutputPath, "Smartstore.Module.Smartstore.PayPal.6.0.0.zip")), Is.True);
    }

    [Test]
    public async Task Creates_output_directory_on_demand()
    {
        using var artifact = new TempArtifactRoot();
        artifact.AddModule("Modules/Smartstore.PayPal", "Smartstore.PayPal");

        Assert.That(Directory.Exists(artifact.OutputPath), Is.False);

        await RunAsync("pack", "--root", artifact.Root, "--output", artifact.OutputPath, "--all");

        Assert.That(Directory.Exists(artifact.OutputPath), Is.True);
    }

    private Task<int> RunAsync(params string[] args)
        => new PackagerCliApplication(_out, _error).RunAsync(args);
}
