#nullable enable

using Smartstore.Threading;

namespace Smartstore.Web.Sass;

/// <summary>
/// Provides a shared Sass compiler for the duration of a compilation scope.
/// </summary>
public interface ISassCompilerFactory
{
    /// <summary>
    /// Acquires a compiler lease. Dispose the lease after the last compilation that uses it.
    /// </summary>
    /// <param name="cancellationToken">Cancels waiting to acquire the compiler lease.</param>
    /// <returns>A lease that keeps the shared compiler alive until disposed.</returns>
    ValueTask<ResourceLease<ISassCompiler>> GetCompilerAsync(CancellationToken cancellationToken = default);
}
