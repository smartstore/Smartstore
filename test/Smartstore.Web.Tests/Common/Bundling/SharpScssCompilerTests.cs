#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Moq;
using NUnit.Framework;
using SharpScss;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Sass;

namespace Smartstore.Web.Tests.Common.Bundling;

[TestFixture]
public class SharpScssCompilerTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Preserves_LibSass_output_and_dependencies(bool minify)
    {
        const string source = "$color: #123456; .card { color: $color; &--active { font-weight: 600; } }";
        var expected = Scss.ConvertToCss(source, new ScssOptions
        {
            InputFile = "/styles/site.scss",
            OutputStyle = minify ? ScssOutputStyle.Compressed : ScssOutputStyle.Nested
        });

        var actual = await new SharpScssCompiler().CompileAsync(new SassCompilationRequest
        {
            Source = source,
            SourcePath = "/styles/site.scss",
            FileProvider = CreateAssetFileProvider(new Dictionary<string, string>()),
            Minify = minify
        });

        Assert.Multiple(() =>
        {
            Assert.That(actual.Css, Is.EqualTo(expected.Css));
            Assert.That(actual.IncludedFiles, Is.EqualTo(expected.IncludedFiles));
            Assert.That(actual.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public async Task Resolves_virtual_partials_before_direct_files_and_deduplicates_imports()
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["/styles/_palette.scss"] = "$color: red;",
            ["/styles/palette.scss"] = "$color: blue;"
        };

        var actual = await new SharpScssCompiler().CompileAsync(new SassCompilationRequest
        {
            Source = "@import 'palette'; @import 'palette'; .target { color: $color; }",
            SourcePath = "/styles/site.scss",
            FileProvider = CreateAssetFileProvider(files)
        });

        Assert.Multiple(() =>
        {
            Assert.That(actual.Css, Does.Contain("color: red"));
            Assert.That(actual.Css, Does.Not.Contain("blue"));
            Assert.That(actual.Css, Does.Contain("deduped: /styles/_palette.scss"));
            Assert.That(actual.IncludedFiles, Does.Contain("/styles/_palette.scss"));
        });
    }

    [Test]
    public async Task Preserves_physical_file_imports()
    {
        const string source = "@import 'palette'; .target { color: $color; }";
        var directory = Path.Combine(Path.GetTempPath(), "smartstore-sass-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var entryPath = Path.Combine(directory, "site.scss");
            File.WriteAllText(entryPath, source);
            File.WriteAllText(Path.Combine(directory, "_palette.scss"), "$color: red;");

            using var provider = new PhysicalFileProvider(directory);
            var expected = Scss.ConvertToCss(source, new ScssOptions
            {
                InputFile = entryPath,
                OutputStyle = ScssOutputStyle.Nested
            });

            var actual = await new SharpScssCompiler().CompileAsync(new SassCompilationRequest
            {
                Source = source,
                SourcePath = "site.scss",
                FileProvider = provider
            });

            Assert.Multiple(() =>
            {
                Assert.That(actual.Css, Is.EqualTo(expected.Css));
                Assert.That(actual.IncludedFiles, Is.EqualTo(expected.IncludedFiles));
            });
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public void Preserves_SharpScss_exception_type_and_message()
    {
        const string source = ".target { color: $missing; }";
        var expected = Assert.Throws<ScssException>(() => Scss.ConvertToCss(source, new ScssOptions
        {
            InputFile = "/styles/site.scss",
            OutputStyle = ScssOutputStyle.Nested
        }));

        var actual = Assert.ThrowsAsync<ScssException>(async () => await new SharpScssCompiler().CompileAsync(new SassCompilationRequest
        {
            Source = source,
            SourcePath = "/styles/site.scss",
            FileProvider = CreateAssetFileProvider(new Dictionary<string, string>())
        }));

        Assert.Multiple(() =>
        {
            Assert.That(actual!.Message, Is.EqualTo(expected!.Message));
            Assert.That(actual.File, Is.EqualTo(expected.File));
            Assert.That(actual.Line, Is.EqualTo(expected.Line));
            Assert.That(actual.Column, Is.EqualTo(expected.Column));
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
            info.SetupGet(x => x.PhysicalPath).Returns(path);
            info.Setup(x => x.CreateReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(content)));
            return info.Object;
        });

        return provider.Object;
    }
}
