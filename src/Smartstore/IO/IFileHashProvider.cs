using System;
using System.Threading.Tasks;

namespace Smartstore.IO
{
    /// <summary>
    /// Supplies a hash code for a file entry, using a custom hash function.
    /// </summary>
    public interface IFileHashProvider
    {
        Task<int> GetFileHashAsync();
    }
}
