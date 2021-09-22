using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Smartstore.Core;
using Smartstore.Engine;
using Smartstore.Utilities;

namespace Smartstore.Pdf.WkHtml
{
    public class WkHtmlToPdfConverter
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
        private bool _batchMode = false;

        private readonly IWkHtmlCommandBuilder _commandBuilder;
        private readonly IApplicationContext _appContext;
        private readonly WkHtmlToPdfOptions _options;

        public WkHtmlToPdfConverter(IWkHtmlCommandBuilder commandBuilder, IApplicationContext appContext, IOptions<WkHtmlToPdfOptions> options)
        {
            _commandBuilder = commandBuilder;
            _appContext = appContext;
            _options = options.Value;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

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
            if (!_batchMode)
            {
                EnsureWkHtmlLibs();
            }

            CheckProcess();
            
            IReadOnlyList<PdfInput> inputs = null;

            try
            {
                using var psb = StringBuilderPool.Instance.Get(out var sb);
                inputs = await _commandBuilder.BuildCommandAsync(settings, sb);

                var arguments = sb.ToString();

                if (_batchMode)
                {
                    await InvokeProcessInBatchAsync(arguments);
                }
                else
                {
                    await InvokeProcessAsync(arguments, settings.Page, output);
                }

                // TODO: (core) Copy input to output stream
            }
            catch (Exception ex)
            {
                if (!_batchMode)
                {
                    EnsureProcessStopped();
                }

                Logger.Error(ex, $"Html to Pdf conversion error: {ex.Message}.");
                throw;

            }
            finally
            {
                if (inputs != null)
                {
                    inputs.Each(x => x.Teardown());
                }
            }
        }

        public IAsyncDisposable BeginBatch()
        {
            if (_batchMode)
            {
                throw new InvalidOperationException("PdfConverter is already in batch mode.");
            }

            _batchMode = true;
            EnsureWkHtmlLibs();

            return new AsyncActionDisposable(EndBatchAsync);
        }

        public async ValueTask EndBatchAsync()
        {
            if (!_batchMode)
            {
                throw new InvalidOperationException("PdfConverter is not in batch mode.");
            }

            _batchMode = false;

            if (_process != null)
            {
                if (!_process.HasExited)
                {
                    _process.StandardInput.Close();
                    await _process.WaitForExitAsync();
                    _process.Close();
                }

                _process = null;
            }
        }

        #region WkHtml utilities

        private Task InvokeProcessAsync(string arguments, PdfInput input, Stream output)
        {
            // TODO: (core) Implement InvokeProcessAsync()
            return Task.CompletedTask;
        }

        private Task InvokeProcessInBatchAsync(string arguments)
        {
            // TODO: (core) Implement InvokeProcessInBatchAsync()
            return Task.CompletedTask;
        }

        private void EnsureWkHtmlLibs()
        {
            // TODO: (core) Implement EnsureWkHtmlLibs()
        }

        private void CheckProcess()
        {
            if (!_batchMode && _process != null)
            {
                throw new InvalidOperationException("WkHtmlToPdf process has already been started.");
            }
        }

        private static void CheckExitCode(int exitCode, string lastErrLine, bool outputNotEmpty)
        {
            if (exitCode != 0 && (exitCode != 1 || Array.IndexOf(_ignoreErrLines, lastErrLine.Trim()) < 0 || !outputNotEmpty))
            {
                throw new WkHtmlToPdfException(exitCode, lastErrLine);
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

        private async Task WaitForFileAsync(string fullPath, TimeSpan? timeout)
        {
            double num = (timeout.HasValue && (timeout.Value != TimeSpan.Zero)) ? timeout.Value.TotalMilliseconds : 60000.0;
            int num2 = 0;
            while (num > 0.0)
            {
                num2++;
                num -= 50.0;
                try
                {
                    using FileStream stream = new(fullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 100);
                    stream.ReadByte();
                    break;
                }
                catch (Exception)
                {
                    await Task.Delay((num2 < 10) ? 50 : 100);
                    continue;
                }
            }
            if (((num == 0.0) && (_process != null)) && !_process.HasExited)
            {
                _process.StandardInput.Close();
                await _process.WaitForExitAsync();
            }

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
