using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Smartstore.Engine;
using Smartstore.Engine.Runtimes;
using Smartstore.IO;
using Smartstore.Pdf;
using Smartstore.Pdf.WkHtml;
using Smartstore.Threading;

// This probe uses the real manager, installer, command builder and converter.
// Only web hosting/DI context is substituted; no database or running shop is needed.
if (!OperatingSystem.IsLinux() || args.Length != 2 || args[0] is not ("system" or "lazy"))
{
    throw new ArgumentException("Usage on Linux: Probe system|lazy <absolute .temp work directory>");
}

var work = Path.GetFullPath(args[1]);
if (!work.Contains("/.temp/", StringComparison.Ordinal))
{
    throw new ArgumentException("Probe output must be below .temp.");
}
Directory.CreateDirectory(work);
using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole());
var host = new Mock<IHostEnvironment>();
host.SetupGet(x => x.ContentRootPath).Returns("/work");
var runtime = new RuntimeInfo(host.Object);
var contentRoot = new Mock<IFileSystem>();
contentRoot.SetupGet(x => x.Root).Returns("/work");
var tempDirectory = new Mock<IDirectory>();
tempDirectory.SetupGet(x => x.PhysicalPath).Returns(work);
var app = new Mock<IApplicationContext>();
app.SetupGet(x => x.RuntimeInfo).Returns(runtime);
app.SetupGet(x => x.ContentRoot).Returns(contentRoot.Object);
app.Setup(x => x.GetTempDirectory(It.IsAny<string>())).Returns(tempDirectory.Object);
var manager = new NativeLibraryManager(app.Object, loggerFactory);
var containerBuilder = new ContainerBuilder();
containerBuilder.RegisterInstance(manager).As<INativeLibraryManager>();
containerBuilder.RegisterInstance(loggerFactory);
containerBuilder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>));
using var container = containerBuilder.Build();
app.SetupGet(x => x.Services).Returns(container);
var engine = new Mock<IEngine>();
engine.SetupGet(x => x.Application).Returns(app.Object);
EngineContext.Replace(engine.Object);

var executable = manager.GetNativeExecutable("wkhtmltopdf", "0.12.6.1");
if (args[0] == "system")
{
    if (!executable.Exists || executable.FullName != "/usr/local/bin/wkhtmltopdf")
    {
        throw new InvalidOperationException("System tool was not selected by NativeLibraryManager.");
    }
}
else
{
    if (executable.Exists)
    {
        throw new InvalidOperationException("Lazy probe requires a fresh image without an installed tool.");
    }
    using var installer = manager.CreateLibraryInstaller();
    executable = await installer.InstallFromPackageAsync(new InstallNativePackageRequest("wkhtmltopdf", true, "Smartstore.wkhtmltopdf.Native")
    {
        MinVersion = "0.12.6.1",
        MaxVersion = "0.12.6.1"
    });
    if (!executable.Exists || executable.DirectoryName != runtime.BaseDirectory)
    {
        throw new InvalidOperationException("Lazy installer did not deploy the expected executable.");
    }
}
Console.WriteLine($"SELECTED mode={args[0]} rid={runtime.RID} path={executable.FullName}");
Console.WriteLine($"SHA256 {Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(executable.FullName)))}");

var httpContext = new HttpContextAccessor();
var lifetime = new Mock<IHostApplicationLifetime>();
var runner = new AsyncRunner(lifetime.Object, Mock.Of<ILifetimeScopeAccessor>());
var options = Options.Create(new WkHtmlToPdfOptions
{
    TempFilesPath = Path.Combine(work, "converter-temp"),
    ExecutionTimeout = TimeSpan.FromSeconds(60)
});
var inputs = new List<string> { "invoice.html" };
if (File.Exists(Path.Combine(work, "https.html")))
{
    inputs.Add("https.html");
}
foreach (var input in inputs)
{
    var converter = new WkHtmlToPdfConverter(new WkHtmlCommandBuilder(httpContext), httpContext,
        Mock.Of<IHttpClientFactory>(), options, runner, loggerFactory.CreateLogger<WkHtmlToPdfConverter>());
    var settings = new PdfConversionSettings
    {
        Title = "Smartstore wkhtml integration probe",
        Page = converter.CreateHtmlInput(await File.ReadAllTextAsync(Path.Combine(work, input))),
        Header = converter.CreateHtmlInput(await File.ReadAllTextAsync(Path.Combine(work, "header.html"))),
        Footer = converter.CreateHtmlInput(await File.ReadAllTextAsync(Path.Combine(work, "footer.html"))),
        Margins = new PdfPageMargins { Top = 25, Bottom = 25 },
        CustomArguments = "--enable-local-file-access --javascript-delay 500 --load-error-handling abort --load-media-error-handling abort"
    };
    await using var pdf = await converter.GeneratePdfAsync(settings);
    await using var output = File.Create(Path.Combine(work, Path.ChangeExtension(input, ".pdf")));
    await pdf.CopyToAsync(output);
}
