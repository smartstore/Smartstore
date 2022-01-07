using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Smartstore.Data
{
    internal class NullLazyLoader : ILazyLoader
    {
        public static ILazyLoader Instance => new NullLazyLoader();

        private NullLazyLoader()
        {
        }

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
