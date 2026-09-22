#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Moq;
using NUnit.Framework;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Sass;

namespace Smartstore.Web.Tests.Common.Bundling;

[TestFixture]
public class DartSassCompilerTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Compiles_virtual_imports_and_tracks_dependencies(bool minify)
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["/shared/_color.scss"] = "$color: red;",
            ["/styles/_local.scss"] = ".local { color: $color; }"
        };

        await using var compiler = new DartSassCompiler();
        var result = await compiler.CompileAsync(new SassCompilationRequest
        {
            Source = "@import '/shared/color'; @import 'local'; .target { color: $color; }",
            SourcePath = "/styles/site.scss",
            FileProvider = CreateAssetFileProvider(files),
            Minify = minify
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Css, Does.Contain(minify ? "color:red" : "color: red"));
            Assert.That(result.IncludedFiles, Is.EquivalentTo(new[] { "/shared/_color.scss", "/styles/_local.scss" }));
            Assert.That(result.Diagnostics, Has.Some.Matches<SassDiagnostic>(x => x.Severity == SassDiagnosticSeverity.DeprecationWarning && x.Code == "import"));
            if (minify)
            {
                Assert.That(result.Css, Does.Not.Contain("color: red;"));
            }
        });
    }

    [Test]
    public async Task Resolves_nested_relative_virtual_imports()
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["/styles/nested/_first.scss"] = "@import '../second';",
            ["/styles/_second.scss"] = ".nested { color: blue; }"
        };

        await using var compiler = new DartSassCompiler();
        var result = await compiler.CompileAsync(new SassCompilationRequest
        {
            Source = "@import 'nested/first';",
            SourcePath = "/styles/site.scss",
            FileProvider = CreateAssetFileProvider(files)
        });

        Assert.That(result.Css, Does.Contain("color: blue"));
        Assert.That(result.IncludedFiles, Is.EquivalentTo(new[] { "/styles/nested/_first.scss", "/styles/_second.scss" }));
    }

    [Test]
    public async Task Snapshots_request_scoped_virtual_imports_before_compilation()
    {
        var themeVarsLookups = 0;
        var moduleImportsLookups = 0;
        var provider = new Mock<IAssetFileProvider>();
        provider.Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns((string path) =>
        {
            string? content = path switch
            {
                "/.app/themevars.scss" => ReadOnce(ref themeVarsLookups, "$brand: red;"),
                "/.app/moduleimports.scss" => ReadOnce(ref moduleImportsLookups, "@import '/styles/extra';"),
                "/styles/_extra.scss" => ".extra { color: $brand; }",
                _ => null
            };

            if (content is null)
            {
                return new NotFoundFileInfo(path);
            }

            var info = new Mock<IFileInfo>();
            info.SetupGet(x => x.Exists).Returns(true);
            info.Setup(x => x.CreateReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(content)));
            return info.Object;
        });

        await using var compiler = new DartSassCompiler();
        var result = await compiler.CompileAsync(new SassCompilationRequest
        {
            Source = "@import '/.app/themevars.scss'; @import '/.app/moduleimports.scss'; .target { color: $brand; }",
            SourcePath = "/styles/site.scss",
            FileProvider = provider.Object
        });

        Assert.Multiple(() =>
        {
            Assert.That(themeVarsLookups, Is.EqualTo(1));
            Assert.That(moduleImportsLookups, Is.EqualTo(1));
            Assert.That(result.Css, Does.Contain(".extra"));
            Assert.That(result.Css, Does.Contain("color: red"));
            Assert.That(result.IncludedFiles, Does.Contain("/.app/themevars.scss"));
            Assert.That(result.IncludedFiles, Does.Contain("/.app/moduleimports.scss"));
            Assert.That(result.IncludedFiles, Does.Contain("/styles/_extra.scss"));
        });

        static string ReadOnce(ref int count, string content)
        {
            if (++count != 1)
            {
                throw new InvalidOperationException("A request-scoped virtual file was accessed from an importer callback.");
            }

            return content;
        }
    }

    [Test]
    public async Task Resolves_physical_file_imports()
    {
        var directory = Path.Combine(Path.GetTempPath(), "smartstore-dart-sass-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(Path.Combine(directory, "site.scss"), "@import 'palette'; .target { color: $color; }");
            File.WriteAllText(Path.Combine(directory, "_palette.scss"), "$color: green;");

            using var provider = new PhysicalFileProvider(directory);
            await using var compiler = new DartSassCompiler();
            var result = await compiler.CompileAsync(new SassCompilationRequest
            {
                Source = "@import 'palette'; .target { color: $color; }",
                SourcePath = "site.scss",
                FileProvider = provider
            });

            Assert.That(result.Css, Does.Contain("color: green"));
            Assert.That(result.IncludedFiles, Does.Contain(Path.Combine(directory, "_palette.scss")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task Returns_warning_and_debug_diagnostics()
    {
        await using var compiler = new DartSassCompiler();
        var result = await compiler.CompileAsync(new SassCompilationRequest
        {
            Source = "@warn 'careful'; @debug 'value'; .target { color: red; }",
            SourcePath = "/styles/site.scss",
            FileProvider = CreateAssetFileProvider(new Dictionary<string, string>())
        });

        Assert.That(result.Diagnostics, Has.Some.Matches<SassDiagnostic>(x => x.Severity == SassDiagnosticSeverity.Warning && x.Message.Contains("careful")));
        Assert.That(result.Diagnostics, Has.Some.Matches<SassDiagnostic>(x => x.Severity == SassDiagnosticSeverity.Debug && x.Message.Contains("value")));
        Assert.That(result.Diagnostics, Has.Some.Matches<SassDiagnostic>(x => x.Severity == SassDiagnosticSeverity.Warning
            && x.FormattedMessage is { } formatted
            && formatted.Contains("/styles/site.scss")
            && !formatted.Contains("smsass:")));
    }

    [Test]
    public void Preserves_compilation_error_details()
    {
        var error = Assert.ThrowsAsync<SassCompilationException>(async () =>
        {
            await using var compiler = new DartSassCompiler();
            await compiler.CompileAsync(new SassCompilationRequest
            {
                Source = ".target { color: $missing; }",
                SourcePath = "/styles/site.scss",
                FileProvider = CreateAssetFileProvider(new Dictionary<string, string>())
            });
        });

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Does.StartWith("Error: Undefined variable."));
            Assert.That(error.Message, Does.Contain("/styles/site.scss 1:"));
            Assert.That(error.Message, Does.Contain(".target { color: $missing; }"));
            Assert.That(error.Message, Does.Match(@"\^\^{2,}"));
            Assert.That(error.Diagnostic.SourcePath, Is.EqualTo("/styles/site.scss"));
            Assert.That(error.Diagnostic.Line, Is.EqualTo(1));
            Assert.That(error.Message, Is.EqualTo(error.Diagnostic.FormattedMessage));
            Assert.That(error.Message, Does.Not.Contain("smsass:"));
            Assert.That(error.Message, Does.Not.Contain("\u2577"));
            Assert.That(error.InnerException, Is.TypeOf<EmbeddedSass.Diagnostics.SassCompilationException>());
        });
    }

    [Test]
    public void Reports_error_location_in_imported_partial()
    {
        var files = new Dictionary<string, string>
        {
            ["/styles/_broken.scss"] = ".broken { color: (); }"
        };

        var error = Assert.ThrowsAsync<SassCompilationException>(async () =>
        {
            await using var compiler = new DartSassCompiler();
            await compiler.CompileAsync(new SassCompilationRequest
            {
                Source = "@import 'broken';",
                SourcePath = "/styles/site.scss",
                FileProvider = CreateAssetFileProvider(files)
            });
        });

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Does.Contain("() isn't a valid CSS value"));
            Assert.That(error.Message, Does.Contain("/styles/_broken.scss 1:"));
            Assert.That(error.Message, Does.Contain(".broken { color: (); }"));
            Assert.That(error.Message, Does.Match(@"\^\^"));
            Assert.That(error.Diagnostic.SourcePath, Is.EqualTo("/styles/_broken.scss"));
            Assert.That(error.Diagnostic.Line, Is.EqualTo(1));
            Assert.That(error.Diagnostic.FormattedMessage, Does.Contain(".broken { color: (); }"));
            Assert.That(error.Message, Does.Not.Contain("smsass:"));
        });
    }

    [Test]
    public void Reports_parent_import_location()
    {
        var files = new Dictionary<string, string>
        {
            ["/styles/_broken.scss"] = ".broken { color: $missing; }"
        };

        var error = Assert.ThrowsAsync<SassCompilationException>(async () =>
        {
            await using var compiler = new DartSassCompiler();
            await compiler.CompileAsync(new SassCompilationRequest
            {
                Source = "@import 'broken';",
                SourcePath = "/styles/site.scss",
                FileProvider = CreateAssetFileProvider(files)
            });
        });

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Does.StartWith("Error: Undefined variable."));
            Assert.That(error.Message, Does.Contain("/styles/_broken.scss 1:18"));
            Assert.That(error.Message, Does.Contain("/styles/site.scss 1:9"));
            Assert.That(error.Message, Does.Contain(".broken { color: $missing; }"));
            Assert.That(error.Message, Does.Contain("^^^^^^^^"));
            Assert.That(error.Message, Does.Not.Contain("smsass:"));
        });
    }

    private static IAssetFileProvider CreateAssetFileProvider(IReadOnlyDictionary<string, string> files)
    {
        var provider = new Mock<IAssetFileProvider>();
        provider.Setup(x => x.GetFileInfo(It.IsAny<string>())).Returns((string path) =>
        {
            if (!files.TryGetValue(path, out var content))
            {
                return new NotFoundFileInfo(path);
            }

            var info = new Mock<IFileInfo>();
            info.SetupGet(x => x.Exists).Returns(true);
            info.Setup(x => x.CreateReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(content)));
            return info.Object;
        });

        return provider.Object;
    }
}
