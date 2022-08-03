using System.Diagnostics;
using System.Net.Http;
using System.Text;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Smartstore.Diagnostics;
using Smartstore.Engine;
using Smartstore.Engine.Runtimes;
using Smartstore.Http;
using Smartstore.Threading;
using Smartstore.Utilities;

namespace Smartstore.Pdf.WkHtml
{
    // TODO: (core) Implement BatchMode
    public class WkHtmlToPdfConverter : IPdfConverter
    {
        private static string _tempPath;
        private static AsyncLazy<string> _toolName = new(GetToolNameAsync);
        //private readonly static string[] _ignoreErrLines = new string[] 
        //{ 
        //    "Exit with code 1 due to network error: ContentNotFoundError", 
        //    "QFont::setPixelSize: Pixel size <= 0", 
        //    "Exit with code 1 due to network error: ProtocolUnknownError", 
        //    "Exit with code 1 due to network error: HostNotFoundError", 
        //    "Exit with code 1 due to network error: ContentOperationNotPermittedError", 
        //    "Exit with code 1 due to network error: UnknownContentError" 
        //};

        private Process _process;

        private readonly IWkHtmlCommandBuilder _commandBuilder;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly WkHtmlToPdfOptions _options;
        private readonly AsyncRunner _asyncRunner;

        public WkHtmlToPdfConverter(
            IWkHtmlCommandBuilder commandBuilder,
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory,
            IOptions<WkHtmlToPdfOptions> options,
            AsyncRunner asyncRunner,
            ILogger<WkHtmlToPdfConverter> logger)
        {
            _commandBuilder = commandBuilder;
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _asyncRunner = asyncRunner;

            Logger = logger;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        private static async Task<string> GetToolNameAsync()
        {
            var services = EngineContext.Current.Application.Services;
            var message = @"Unable to install PDF processor tool 'wkhtmltopdf'. PDF documents may not be generated unless you manually install 'wkhtmltopdf' on your web server. Please contact your hosting provider or system administrator to install the appropriate package for your operating system. See: https://wkhtmltopdf.org/downloads.html";
            var toolName = "wkhtmltopdf";

            try
            {
                var libraryManager = services.Resolve<INativeLibraryManager>();

                var fi = libraryManager.GetNativeExecutable("wkhtmltopdf");

                if (!fi.Exists)
                {
                    using var libraryInstaller = libraryManager.CreateLibraryInstaller();
                    fi = await libraryInstaller.InstallFromPackageAsync(new InstallNativePackageRequest("wkhtmltopdf", true, "Smartstore.wkhtmltopdf.Native"));
                }

                if (!fi.Exists)
                {
                    GetLogger().Warn(message);
                }
                else
                {
                    toolName = fi.FullName;
                }
            }
            catch (Exception ex)
            {
                GetLogger().Warn(ex, message);
            }

            return toolName;

            ILogger GetLogger()
            {
                return services.Resolve<ILogger<WkHtmlToPdfConverter>>();
            }
        }

        /// <summary>
        /// Occurs when log line is received from WkHtmlToPdf process
        /// </summary>
        /// <remarks>
        /// Quiet mode should be disabled if you want to get wkhtmltopdf info/debug messages
        /// </remarks>
        public event EventHandler<DataReceivedEventArgs> LogReceived;

        public virtual IPdfInput CreateFileInput(string urlOrPath, bool prefetch = false)
        {
            Guard.NotEmpty(urlOrPath, nameof(urlOrPath));

            if (prefetch && (urlOrPath.IsWebUrl() || WebHelper.IsLocalUrl(urlOrPath)))
            {
                var content = PrefetchFileInput(urlOrPath);
                return CreateHtmlInput(content);
            }

            return new WkFileInput(urlOrPath, _options, _httpContextAccessor.HttpContext);
        }

        private string PrefetchFileInput(string url)
        {
            var fileInput = new WkFileInput(url, _options, _httpContextAccessor.HttpContext);
            // Make url absolute
            url = fileInput.Content;

            var httpClient = _httpClientFactory.CreateClient("local");
            // Download content
            var content = httpClient.GetStringAsync(url).Await();

            return content;
        }

        public virtual IPdfInput CreateHtmlInput(string html)
        {
            Guard.NotEmpty(html, nameof(html));
            return new WkHtmlInput(html, _options, _httpContextAccessor.HttpContext);
        }

        public Task<Stream> GeneratePdfAsync(PdfConversionSettings settings, CancellationToken cancelToken = default)
        {
            Guard.NotNull(settings, nameof(settings));

            if (settings.Page == null)
            {
                throw new ArgumentException($"The '{nameof(settings.Page)}' property of the '{nameof(settings)}' argument cannot be null.", nameof(settings));
            }

            return GeneratePdfCoreAsync(settings, cancelToken);
        }

        protected virtual async Task<Stream> GeneratePdfCoreAsync(PdfConversionSettings settings, CancellationToken cancelToken = default)
        {
            // Check that process is not already running
            CheckProcess();

            try
            {
                // Build command / arguments
                using var psb = StringBuilderPool.Instance.Get(out var sb);
                await _commandBuilder.BuildCommandAsync(settings, sb);

                // Create output PDF temp file name
                var outputFileName = GetTempFileName(_options, ".pdf");
                sb.AppendFormat(" \"{0}\" ", outputFileName);

                var arguments = sb.ToString();
                var compositeCancelToken = CreateCancellationToken(cancelToken);

                // Run process
                await RunProcessAsync(arguments, settings.Page, compositeCancelToken);

                compositeCancelToken.ThrowIfCancellationRequested();

                // Return wkhtml output file as temp file stream (auto-deletes on close)
                if (File.Exists(outputFileName))
                {
                    return new FileStream(outputFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
                }
                else
                {
                    throw new FileNotFoundException($"PDF converter cannot find output file '{outputFileName}'.");
                }
            }
            catch (Exception ex) when (ex is (ProcessException or FileNotFoundException))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ProcessException($"wkhtmltopdf error: {ex.Message}.", ex, null);
            }
            finally
            {
                // Teardown / clear inputs
                settings.Page?.Teardown();
                settings.Header?.Teardown();
                settings.Footer?.Teardown();
                settings.Cover?.Teardown();
            }
        }

        private async Task RunProcessAsync(string arguments, IPdfInput input, CancellationToken cancelToken)
        {
            var data = new List<string>();

            DataReceivedEventHandler onDataReceived = (o, e) =>
            {
                if (e.Data.HasValue())
                {
                    data.Add(e.Data);
                    LogReceived?.Invoke(this, e);
                }
            };

            try
            {
                var toolName = await _toolName;

                Logger.Debug($"Starting process '{toolName}' with arguments '{arguments}'.");

                _process = Process.Start(new ProcessStartInfo
                {
                    FileName = toolName,
                    Arguments = arguments,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    StandardInputEncoding = Encoding.UTF8,
                    RedirectStandardInput = input.Kind == PdfInputKind.Html,
                    RedirectStandardOutput = false,
                    RedirectStandardError = true
                });

                if (_options.ProcessPriority != ProcessPriorityClass.Normal)
                {
                    _process.PriorityClass = _options.ProcessPriority;
                }

                _process.ErrorDataReceived += onDataReceived;
                _process.BeginErrorReadLine();

                if (input.Kind == PdfInputKind.Html)
                {
                    using var sIn = _process.StandardInput;
                    sIn.WriteLine(input.Content);
                }

                await _process.WaitForExitAsync(cancelToken);

                if (data.Count > 0)
                {
                    throw new ProcessException(_process, data);
                }
            }
            finally
            {
                EnsureProcessStopped();
            }
        }

        #region WkHtml utilities

        internal static string GetTempFileName(WkHtmlToPdfOptions options, string extension)
        {
            return Path.Combine(GetTempPath(options), "pdfgen-" + Path.GetRandomFileName() + extension.EmptyNull());
        }

        internal static string GetTempPath(WkHtmlToPdfOptions options)
        {
            LazyInitializer.EnsureInitialized(ref _tempPath, () =>
            {
                if (options.TempFilesPath.HasValue() && !Directory.Exists(options.TempFilesPath))
                {
                    Directory.CreateDirectory(options.TempFilesPath);
                }

                return options.TempFilesPath ?? Path.GetTempPath();
            });

            return _tempPath;
        }

        private void CheckProcess()
        {
            if (_process != null)
            {
                throw new InvalidOperationException("WkHtmlToPdf process has already been started.");
            }
        }

        private CancellationToken CreateCancellationToken(CancellationToken userCancelToken = default)
        {
            var result = _asyncRunner.CreateCompositeCancellationToken(userCancelToken);
            if (_options.ExecutionTimeout.HasValue)
            {
                var cts = new CancellationTokenSource(_options.ExecutionTimeout.Value);
                result = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, result).Token;
            }

            return result;
        }

        private void EnsureProcessStopped()
        {
            if (_process != null)
            {
                _process.EnsureStopped();
                _process = null;
            }
        }

        #endregion
    }
}
