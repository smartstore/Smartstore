using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Smartstore.Engine;
using Smartstore.Utilities;

namespace Smartstore.Pdf.WkHtml
{
    // TODO: (core) Implement BatchMode
    public class WkHtmlToPdfConverter : IPdfConverter
    {
        private readonly static object _lock = new();
        private readonly static string[] _ignoreErrLines = new string[] 
        { 
            "Exit with code 1 due to network error: ContentNotFoundError", 
            "QFont::setPixelSize: Pixel size <= 0", 
            "Exit with code 1 due to network error: ProtocolUnknownError", 
            "Exit with code 1 due to network error: HostNotFoundError", 
            "Exit with code 1 due to network error: ContentOperationNotPermittedError", 
            "Exit with code 1 due to network error: UnknownContentError" 
        };

        private Process _process;

        private readonly IWkHtmlCommandBuilder _commandBuilder;
        private readonly IApplicationContext _appContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly WkHtmlToPdfOptions _options;

        public WkHtmlToPdfConverter(
            IWkHtmlCommandBuilder commandBuilder, 
            IApplicationContext appContext,
            IHttpContextAccessor httpContextAccessor,
            IOptions<WkHtmlToPdfOptions> options)
        {
            _commandBuilder = commandBuilder;
            _appContext = appContext;
            _httpContextAccessor = httpContextAccessor;
            _options = options.Value;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        /// <summary>
        /// Occurs when log line is received from WkHtmlToPdf process
        /// </summary>
        /// <remarks>
        /// Quiet mode should be disabled if you want to get wkhtmltopdf info/debug messages
        /// </remarks>
        public event EventHandler<DataReceivedEventArgs> LogReceived;

        public virtual IPdfInput CreateUrlInput(string url)
        {
            Guard.NotEmpty(url, nameof(url));
            return new WkUrlInput(url, _options, _httpContextAccessor.HttpContext);
        }

        public virtual IPdfInput CreateHtmlInput(string html)
        {
            Guard.NotEmpty(html, nameof(html));
            return new WkHtmlInput(html, _options, _httpContextAccessor.HttpContext);
        }

        public Task ConvertAsync(PdfConversionSettings settings, Stream output)
        {
            Guard.NotNull(settings, nameof(settings));
            Guard.NotNull(output, nameof(output));

            if (settings.Page == null)
            {
                throw new ArgumentException($"The '{nameof(settings.Page)}' property of the '{nameof(settings)}' argument cannot be null.", nameof(settings));
            }

            return GeneratePdfAsync(settings, output);
        }

        protected virtual async Task GeneratePdfAsync(PdfConversionSettings settings, Stream output)
        {
            // Ensure that native library is available
            EnsureWkHtmlLibs();

            // Check that process is not already running
            CheckProcess();
            
            string outputPdfFileName = null;

            try
            {
                // Build command / arguments
                using var psb = StringBuilderPool.Instance.Get(out var sb);
                await _commandBuilder.BuildCommandAsync(settings, sb);

                // Create output PDF temp file name
                outputPdfFileName = Path.Combine(GetTempPath(), "pdfgen-" + Path.GetRandomFileName() + ".pdf");
                sb.AppendFormat(" \"{0}\" ", outputPdfFileName);

                var arguments = sb.ToString();

                // Invoke process
                await InvokeProcessAsync(arguments, settings.Page, output);

                // Copy wkhtml output from temp file to given output stream
                using var fileStream = new FileStream(outputPdfFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                await fileStream.CopyToAsync(output);
            }
            catch (Exception ex)
            {
                EnsureProcessStopped();

                Logger.Error(ex, $"Html to Pdf conversion error: {ex.Message}.");
                throw;

            }
            finally
            {
                // Delete output temp file
                if (outputPdfFileName.HasValue() && File.Exists(outputPdfFileName))
                {
                    try
                    {
                        File.Delete(outputPdfFileName);
                    }
                    catch
                    {
                    }
                }

                // Teardown / clear inputs
                settings.Page.Teardown();
                settings.Header?.Teardown();
                settings.Footer?.Teardown();
                settings.Cover?.Teardown();
            }
        }

        #region WkHtml utilities

        private async Task InvokeProcessAsync(string arguments, IPdfInput input, Stream output)
        {
            var lastErrorLine = string.Empty;
            DataReceivedEventHandler onDataReceived = ((o, e) =>
            {
                if (e.Data == null) return;
                if (e.Data.HasValue()) 
                {
                    lastErrorLine = e.Data;
                    Logger.Debug("WkHtml data received: {0}.", e.Data);
                }
                LogReceived?.Invoke(this, e);
            });

            try
            {
                _process = Process.Start(new ProcessStartInfo
                {
                    FileName = GetToolExePath(),
                    WorkingDirectory = _options.PdfToolPath,
                    Arguments = arguments,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

                if (_options.ProcessPriority != ProcessPriorityClass.Normal)
                {
                    _process.PriorityClass = _options.ProcessPriority;
                }  
                if (_options.ProcessProcessorAffinity.HasValue)
                {
                    _process.ProcessorAffinity = _options.ProcessProcessorAffinity.Value;
                }

                _process.ErrorDataReceived += onDataReceived;
                _process.BeginErrorReadLine();

                if (input.Kind == PdfInputKind.Html)
                {
                    using var sIn = _process.StandardInput;
                    sIn.WriteLine(input.Content);
                }

                await ReadStdOutToStreamAsync(_process, output);
                await WaitForExitAsync(_options.ExecutionTimeout);
            }
            finally
            {
                EnsureProcessStopped();
            }
        }

        private string GetTempPath()
        {
            // TODO: (core) Make GetTempPath() globally available
            if (_options.TempFilesPath.HasValue() && !Directory.Exists(_options.TempFilesPath))
            {
                Directory.CreateDirectory(_options.TempFilesPath);
            }
                
            return _options.TempFilesPath ?? Path.GetTempPath();
        }

        private void EnsureWkHtmlLibs()
        {
            // TODO: (core) Implement EnsureWkHtmlLibs()
        }

        private void CheckProcess()
        {
            if (_process != null)
            {
                throw new InvalidOperationException("WkHtmlToPdf process has already been started.");
            }
        }

        private string GetToolExePath()
        {
            if (_options.PdfToolPath.IsEmpty())
            {
                throw new ArgumentException($"{nameof(_options.PdfToolPath)} property is not initialized with path to wkhtmltopdf binaries.");
            }

            var path = Path.Combine(_options.PdfToolPath, _options.PdfToolName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("wkhtmltopdf executable does not exist. Attempted path: " + path);
            }

            return path;

        }

        private static Task ReadStdOutToStreamAsync(Process proc, Stream output)
        {
            return proc.StandardOutput.BaseStream.CopyToAsync(output);
        }

        private async Task WaitForExitAsync(TimeSpan? timeout)
        {
            if (timeout.HasValue)
            {
                if (!_process.WaitForExit((int)timeout.Value.TotalMilliseconds))
                {
                    EnsureProcessStopped();
                    throw new WkHtmlToPdfException(-2, $"WkHtmlToPdf process exceeded execution timeout ({timeout}) and was aborted");
                }
            }
            else
            {
                await _process.WaitForExitAsync();
            }

        }

        private void EnsureProcessStopped()
        {
            if (_process != null)
            {
                if (!_process.HasExited)
                {
                    try
                    {
                        _process.Kill();
                        _process.Close();
                        _process = null;
                    }
                    catch (Exception)
                    {
                    }
                }
                else
                {
                    _process.Close();
                    _process = null;
                }
            }
        }

        #endregion
    }
}
