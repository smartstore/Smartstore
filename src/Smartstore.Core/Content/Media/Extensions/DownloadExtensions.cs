using System;
using System.Collections.Generic;
using System.Linq;

namespace Smartstore.Core.Content.Media
{
    public static class DownloadExtensions
    {
        /// <summary>
        /// Orders the given download collection by semantic <see cref="Download.FileVersion"/>
        /// </summary>
        /// <param name="downloads">The source collection.</param>
        public static IList<Download> OrderByVersion(this ICollection<Download> downloads, bool descending = true)
        {
            Guard.NotNull(downloads, nameof(downloads));

            if (downloads.Count == 0)
            {
                // INFO: LINQ will internally return the list
                return downloads.ToList();
            }

            // TODO: (core) Implement SemanticVersion
            var orderedIds = downloads
                .Select(x => new { x.Id, Version = new Version(x.FileVersion.HasValue() ? x.FileVersion : "0.0") });

            if (descending)
            {
                orderedIds = orderedIds.OrderByDescending(x => x.Version).AsEnumerable();
            }
            else
            {
                orderedIds = orderedIds.OrderBy(x => x.Version).AsEnumerable();
            }

            return new List<Download>(downloads.OrderBySequence(orderedIds.Select(x => x.Id)));
        }
    }
}
