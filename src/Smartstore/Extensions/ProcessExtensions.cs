using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Smartstore.Core
{
    public static class ProcessExtensions
    {
        public static async Task RunAsync(this Process process, CancellationToken cancelToken = default)
        {
            Guard.NotNull(process, nameof(process));

            process.Start();
            await process.WaitForExitAsync(cancelToken);
        }
    }
}
