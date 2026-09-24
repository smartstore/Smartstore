#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.FileProviders;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Common.Configuration;
using Smartstore.Engine;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Sass;

namespace Smartstore.Web.Tests.Common.Bundling;

[TestFixture]
public class SassCompilerFactoryTests
{
    [Test]
    public async Task Retires_idle_compiler_and_creates_another_on_demand()
    {
        var (factory, container) = CreateFactory(() => false);
        using var _ = container;
        await using var compilerFactory = factory;
        Assert.That(factory.HasCompiler, Is.False);

        await using (var lease = await factory.GetCompilerAsync())
        {
            var result = await lease.Value.CompileAsync(CreateRequest());
            Assert.That(result.Css, Does.Contain("color: red"));
        }

        Assert.That(factory.HasCompiler, Is.True);
        await WaitForRetirementAsync(factory);

        await using var nextLease = await factory.GetCompilerAsync();
        var nextResult = await nextLease.Value.CompileAsync(CreateRequest());
        Assert.That(nextResult.Css, Does.Contain("color: red"));
    }

    [Test]
    public async Task Applies_keep_in_memory_change_on_next_acquisition()
    {
        var keepInMemory = true;
        var (factory, container) = CreateFactory(() => keepInMemory);
        using var _ = container;
        await using var compilerFactory = factory;

        await using (var lease = await factory.GetCompilerAsync())
        {
            await lease.Value.CompileAsync(CreateRequest());
        }

        await Task.Delay(TimeSpan.FromSeconds(5.2));
        Assert.That(factory.HasCompiler, Is.True);

        keepInMemory = false;
        await using (var lease = await factory.GetCompilerAsync())
        {
            await lease.Value.CompileAsync(CreateRequest());
        }

        await WaitForRetirementAsync(factory);
    }

    [Test]
    public async Task Waits_for_the_last_lease_before_retiring()
    {
        var (factory, container) = CreateFactory(() => false);
        using var _ = container;
        await using var compilerFactory = factory;
        await using var first = await factory.GetCompilerAsync();
        await using var second = await factory.GetCompilerAsync();

        Assert.That(second.Value, Is.SameAs(first.Value));
        await first.DisposeAsync();
        await first.DisposeAsync(); // Releasing a lease twice must not affect another holder.
        await Task.Delay(TimeSpan.FromSeconds(5.2));
        Assert.That(factory.HasCompiler, Is.True);

        await second.DisposeAsync();
    }

    [Test]
    public async Task Reacquisition_invalidates_the_previous_idle_timeout()
    {
        var (factory, container) = CreateFactory(() => false);
        using var _ = container;
        await using var compilerFactory = factory;

        var first = await factory.GetCompilerAsync();
        var compiler = first.Value;
        await first.DisposeAsync();

        await Task.Delay(200);
        await using (var second = await factory.GetCompilerAsync())
        {
            Assert.That(second.Value, Is.SameAs(compiler));

            // The first lease's timeout must not retire a compiler held by a later lease.
            await Task.Delay(TimeSpan.FromSeconds(5.2));
            Assert.That(factory.HasCompiler, Is.True);
        }
    }

    private static (DartSassCompilerFactory Factory, IContainer Container) CreateFactory(Func<bool> keepInMemory)
    {
        var builder = new ContainerBuilder();
        builder.Register(_ => new PerformanceSettings { KeepSassCompilerInMemory = keepInMemory() });
        var container = builder.Build();

        var scopeAccessor = new Mock<ILifetimeScopeAccessor>();
        scopeAccessor.SetupGet(x => x.LifetimeScope).Returns(container);

        return (new DartSassCompilerFactory(new Work<PerformanceSettings>(scopeAccessor.Object)), container);
    }

    private static SassCompilationRequest CreateRequest()
    {
        var provider = new Mock<IAssetFileProvider>();
        provider.Setup(x => x.GetFileInfo(It.IsAny<string>()))
            .Returns((string path) => new NotFoundFileInfo(path));

        return new SassCompilationRequest
        {
            Source = ".target { color: red; }",
            SourcePath = "/styles/site.scss",
            FileProvider = provider.Object
        };
    }

    private static async Task WaitForRetirementAsync(DartSassCompilerFactory factory)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (factory.HasCompiler)
        {
            await Task.Delay(20, timeout.Token);
        }
    }
}
