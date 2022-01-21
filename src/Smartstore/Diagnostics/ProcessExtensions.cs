using System.Diagnostics;

namespace Smartstore
{
    public static class ProcessExtensions
    {
        public static async Task RunAsync(this Process process, CancellationToken cancelToken = default)
        {
            Guard.NotNull(process, nameof(process));

            process.Start();
            await process.WaitForExitAsync(cancelToken);
        }

        public static void EnsureStopped(this Process process)
        {
            Guard.NotNull(process, nameof(process));

            if (!process.HasExited)
            {
                try
                {
                    process.Kill();
                    process.Close();
                }
                catch
                {
                }
            }
            else
            {
                process.Close();
            }
        }
    }
}
