using Microsoft.EntityFrameworkCore.ChangeTracking;
using EfState = Microsoft.EntityFrameworkCore.EntityState;

namespace Smartstore.Data
{
    public static class ChangeTrackerExtensions
    {
        internal static IEnumerable<IMergedData> GetMergeableEntities(this ChangeTracker changeTracker)
        {
            return changeTracker.Entries()
                .Where(x => x.State > EfState.Detached)
                .Select(x => x.Entity)
                .OfType<IMergedData>();
        }
    }
}
