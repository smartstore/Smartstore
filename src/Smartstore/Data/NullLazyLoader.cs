using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Smartstore.Data
{
    internal sealed class NullLazyLoader : ILazyLoader
    {
        private NullLazyLoader()
        {
        }

        public static NullLazyLoader Instance { get; } = new NullLazyLoader();

        public void Load(object entity, [CallerMemberName] string navigationName = "")
        {
        }

        public Task LoadAsync(object entity, CancellationToken cancellationToken = default, [CallerMemberName] string navigationName = "")
            => Task.CompletedTask;

        public void SetLoaded(object entity, [CallerMemberName] string navigationName = "", bool loaded = true)
        {
        }

        public void Dispose()
        {
        }
    }
}
