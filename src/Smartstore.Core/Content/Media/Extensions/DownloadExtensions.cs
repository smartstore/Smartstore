namespace Smartstore.Core.Content.Media
{
    public static class DownloadExtensions
    {
        /// <summary>
        /// Orders the given download collection by <see cref="Download.FileVersion"/>
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

            var orderedIds = downloads
                .Select(d =>
                {
                    var strVer = d.FileVersion.NullEmpty() ?? "0.0";
                    if (!SemanticVersion.TryParse(strVer, out var semVer))
                    {
                        semVer = new SemanticVersion(0);
                    }

                    return new { d.Id, Version = semVer };
                });

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
