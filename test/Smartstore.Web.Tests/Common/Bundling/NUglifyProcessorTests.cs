#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NUglify;
using NUnit.Framework;
using Smartstore.Web.Bundling;
using Smartstore.Web.Bundling.Processors;

namespace Smartstore.Web.Tests.Common.Bundling;

[TestFixture]
public class NUglifyProcessorTests
{
    [Test]
    public void Invalidates_bundles_cached_with_the_previous_minifier()
    {
        var httpContext = new DefaultHttpContext();
        var bundle = new ScriptBundle("/bundle/test.js");
        var currentKey = bundle.GetCacheKey(httpContext);
        bundle.ClearProcessors();
        bundle.Processors.Add(new JsMinProcessor());
        bundle.Processors.Add(new ConcatProcessor());

        Assert.That(bundle.GetCacheKey(httpContext).Key, Is.Not.EqualTo(currentKey.Key));
    }

    [Test]
    public async Task Preserves_failed_source_and_continues_minifying_other_files()
    {
        const string invalidSource = "function broken( {";
        const string validSource = "function add(firstOperand, secondOperand) { return firstOperand + secondOperand; }";
        var context = CreateContext();
        context.Content.Add(new AssetContent { Path = "/broken.js", Content = invalidSource });
        context.Content.Add(new AssetContent { Path = "/valid.js", Content = validSource });

        await NUglifyJsMinProcessor.Instance.ProcessAsync(context);

        Assert.That(context.Content[0].Content, Does.StartWith("/* NUGLIFY ERRORS:"));
        Assert.That(context.Content[0].Content, Does.Contain("/broken.js"));
        Assert.That(context.Content[0].Content, Does.EndWith(invalidSource));
        Assert.That(context.Content[1].Content.Length, Is.LessThan(validSource.Length));
        Assert.That(context.Content[1].Content, Does.Not.Contain("firstOperand"));
    }

    [Test]
    public async Task Preserves_source_on_exception_and_escapes_diagnostic_comments()
    {
        const string source = "window.answer = 42;";
        var context = CreateContext();
        context.Content.Add(new AssetContent { Path = "/test.js", Content = source });

        await new ThrowingProcessor().ProcessAsync(context);

        Assert.That(context.Content[0].Content, Does.Contain("Unexpected * / token"));
        Assert.That(context.Content[0].Content, Does.EndWith(" */" + Environment.NewLine + source));
        Assert.That(context.ProcessorCodes, Does.Not.Contain(BundleProcessorCodes.Minify));
    }

    [Test]
    public async Task Leaves_preminified_files_untouched()
    {
        const string source = "window.answer = 42;";
        var context = CreateContext();
        context.Content.Add(new AssetContent { Path = "/vendor.min.js", Content = source });

        await NUglifyJsMinProcessor.Instance.ProcessAsync(context);

        Assert.That(context.Content[0].Content, Is.EqualTo(source));
    }

    [Test]
    public async Task Respects_disabled_minification()
    {
        const string source = "window.answer = 42;";
        var context = CreateContext();
        context.Options.EnableMinification = false;
        context.Content.Add(new AssetContent { Path = "/test.js", Content = source });

        await NUglifyJsMinProcessor.Instance.ProcessAsync(context);

        Assert.That(context.Content[0].Content, Is.EqualTo(source));
        Assert.That(context.ProcessorCodes, Is.Empty);
    }

    [Test]
    public void Supports_lexical_const_and_modern_expressions()
    {
        const string source = """
            function calculate(input) {
                const result = input?.value ?? (input.fallback || 0);
                if (input.enabled) {
                    const result = input.amount;
                    return result;
                }
                return result;
            }
            """;

        var result = NUglifyJsMinProcessor.Instance.MinifyCore(source);

        Assert.That(result.HasErrors, Is.False, string.Join(Environment.NewLine, result.Errors));
        Assert.That(NUglifyJsMinProcessor.Instance.MinifyCore(result.Code).HasErrors, Is.False);
        Assert.That(result.Code, Does.Contain("calculate"));
    }

    [Test]
    public async Task Default_pipeline_minifies_before_concatenating_and_preserves_file_boundaries()
    {
        const string firstSource = "window.first = function(longParameter) { return longParameter + 1; }";
        const string secondSource = "(function() { window.second = window.first(2); })();";
        var context = CreateContext();
        context.Content.Add(new AssetContent { Path = "/first.js", Content = firstSource });
        context.Content.Add(new AssetContent { Path = "/second.js", Content = secondSource });

        foreach (var processor in context.Bundle.Processors)
        {
            await processor.ProcessAsync(context);
        }

        Assert.That(context.Content, Has.Count.EqualTo(1));
        Assert.That(context.Content[0].Content, Does.Not.Contain("longParameter"));
        Assert.That(context.Content[0].Content, Does.Contain(";" + Environment.NewLine));
        Assert.That(NUglifyJsMinProcessor.Instance.MinifyCore(context.Content[0].Content).HasErrors, Is.False);
        Assert.That(context.Bundle.Processors[0], Is.InstanceOf<NUglifyJsMinProcessor>());
    }

    private static BundleContext CreateContext()
    {
        return new BundleContext
        {
            Bundle = new ScriptBundle("/bundle/test.js"),
            Options = new BundlingOptions { EnableMinification = true }
        };
    }

    private sealed class ThrowingProcessor : NUglifyProcessor
    {
        protected internal override UglifyResult MinifyCore(string source)
        {
            throw new InvalidOperationException("Unexpected */ token");
        }
    }
}
