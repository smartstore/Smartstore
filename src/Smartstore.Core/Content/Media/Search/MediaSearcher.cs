using System.Linq.Dynamic.Core;
using Smartstore.Collections;
using Smartstore.Core.Data;
using Smartstore.Core.Rules.Filters;
using Smartstore.Linq;

namespace Smartstore.Core.Content.Media
{
    public class MediaSearcher : IMediaSearcher
    {
        private readonly SmartDbContext _db;
        private readonly Work<IFolderService> _folderService;

        public MediaSearcher(SmartDbContext db, Work<IFolderService> folderService)
        {
            _db = db;
            _folderService = folderService;
        }

        public virtual IPagedList<MediaFile> SearchFiles(MediaSearchQuery query, MediaLoadFlags flags = MediaLoadFlags.AsNoTracking)
        {
            var q = PrepareQuery(query, flags);
            return q.ToPagedList(query.PageIndex, query.PageSize);
        }

        public virtual IQueryable<MediaFile> PrepareQuery(MediaSearchQuery query, MediaLoadFlags flags)
        {
            Guard.NotNull(query);

            var q = _db.MediaFiles.AsQueryable();
            bool? shouldIncludeDeleted = false;

            // Folder
            if (query.FolderId > 0)
            {
                if (query.DeepSearch)
                {
                    var folderIds = _folderService.Value.GetNodesFlattened(query.FolderId.Value, true)
                        .Select(x => x.Id)
                        .ToArray();
                    q = q.Where(x => x.FolderId != null && folderIds.Contains(x.FolderId.Value));
                }
                else
                {
                    q = q.Where(x => x.FolderId == query.FolderId);
                }
            }
            else if (query.FolderId < 0)
            {
                // Special folders
                if (query.FolderId == (int)SpecialMediaFolder.AllFiles)
                {
                    shouldIncludeDeleted = null;
                }
                else if (query.FolderId == (int)SpecialMediaFolder.Trash)
                {
                    shouldIncludeDeleted = true;
                }
                else if (query.FolderId == (int)SpecialMediaFolder.Orphans)
                {
                    // Get ids of untrackable folders, 'cause no orphan check can be made for them.
                    var untrackableFolderIds = _folderService.Value.GetRootNode()
                        .SelectNodes(x => !x.Value.CanDetectTracks)
                        .Select(x => x.Value.Id)
                        .ToArray();

                    q = q.Where(x => x.FolderId > 0 && !untrackableFolderIds.Contains(x.FolderId.Value) && !x.Tracks.Any());
                }
                else if (query.FolderId == (int)SpecialMediaFolder.TransientFiles)
                {
                    q = q.Where(x => x.IsTransient);
                }
                else if (query.FolderId == (int)SpecialMediaFolder.UnassignedFiles)
                {
                    q = q.Where(x => x.FolderId == null);
                }
            }
            else
            {
                // (perf) Composite index
                q = q.Where(x => x.FolderId == null || x.FolderId.HasValue);
            }

            q = ApplyFilterQuery(query, q);

            if (query.Deleted == null && shouldIncludeDeleted.HasValue)
            {
                q = q.Where(x => x.Deleted == shouldIncludeDeleted.Value);
            }

            // Sorting
            var ordering = query.SortBy.NullEmpty() ?? "Id";
            if (query.SortDesc) ordering += " descending";
            q = q.OrderBy(ordering);

            return ApplyLoadFlags(q, flags);
        }

        public virtual IQueryable<MediaFile> ApplyFilterQuery(MediaFilesFilter filter, IQueryable<MediaFile> sourceQuery = null)
        {
            Guard.NotNull(filter);

            var q = sourceQuery ?? _db.MediaFiles.AsQueryable();

            // Term
            if (filter.Term.HasValue() && filter.Term != "*")
            {
                // Convert file pattern to SQL 'LIKE' expression
                q = ApplySearchTerm(q, filter.Term, filter.IncludeAltForTerm, filter.ExactMatch);
            }

            // MediaType
            if (filter.MediaTypes != null && filter.MediaTypes.Length > 0)
            {
                if (filter.MediaTypes.Length == 1)
                {
                    var right = filter.MediaTypes[0];
                    q = q.Where(x => x.MediaType == right);
                }
                else if (filter.MediaTypes.Length > 1)
                {
                    q = q.Where(x => filter.MediaTypes.Contains(x.MediaType));
                }
                else
                {
                    // (perf) Composite index
                    q = q.Where(x => !string.IsNullOrEmpty(x.MediaType));
                }
            }

            // Extension
            if (filter.Extensions != null && filter.Extensions.Length > 0)
            {
                if (filter.Extensions.Length == 1)
                {
                    var right = filter.Extensions[0];
                    q = q.Where(x => x.Extension == right);
                }
                else if (filter.Extensions.Length > 1)
                {
                    q = q.Where(x => filter.Extensions.Contains(x.Extension));
                }
                else
                {
                    // (perf) Composite index
                    q = q.Where(x => !string.IsNullOrEmpty(x.Extension));
                }
            }

            // Tags
            if (filter.Tags != null && filter.Tags.Length > 0)
            {
                q = q.Where(x => x.Tags.Any(t => filter.Tags.Contains(t.Id)));
            }

            // Image dimension
            if (filter.Dimensions != null && filter.Dimensions.Length > 0)
            {
                var predicates = new List<Expression<Func<MediaFile, bool>>>(5);

                foreach (var dim in filter.Dimensions.OrderBy(x => x).Distinct())
                {
                    if (dim == ImageDimension.VerySmall)
                        predicates.Add(x => x.PixelSize > 0 && x.PixelSize <= 50000);
                    else if (dim == ImageDimension.Small)
                        predicates.Add(x => x.PixelSize > 50000 && x.PixelSize <= 250000);
                    else if (dim == ImageDimension.Medium)
                        predicates.Add(x => x.PixelSize > 250000 && x.PixelSize <= 1000000);
                    else if (dim == ImageDimension.Large)
                        predicates.Add(x => x.PixelSize > 1000000 && x.PixelSize <= 2000000);
                    else if (dim == ImageDimension.VeryLarge)
                        predicates.Add(x => x.PixelSize > 2000000);
                }

                if (predicates.Any())
                {
                    var predicate = PredicateBuilder.New(predicates.First());
                    for (var i = 1; i < predicates.Count; i++)
                    {
                        predicate = PredicateBuilder.Or(predicate, predicates[i]);
                    }
                    q = q.Where(predicate);
                }
            }

            // Deleted
            if (filter.Deleted != null)
            {
                q = q.Where(x => x.Deleted == filter.Deleted.Value);
            }

            #region Currently unindexed

            // MimeType
            if (filter.MimeTypes != null && filter.MimeTypes.Length > 0)
            {
                if (filter.MimeTypes.Length == 1)
                {
                    var right = filter.MimeTypes[0];
                    q = q.Where(x => x.MimeType == right);
                }
                else if (filter.MimeTypes.Length > 1)
                {
                    q = q.Where(x => filter.MimeTypes.Contains(x.MimeType));
                }
            }

            // Hidden
            if (filter.Hidden != null)
            {
                q = q.Where(x => x.Hidden == filter.Hidden.Value);
            }

            #endregion

            return q;
        }

        public virtual IQueryable<MediaFile> ApplyLoadFlags(IQueryable<MediaFile> query, MediaLoadFlags flags)
        {
            if (flags == MediaLoadFlags.None)
            {
                return query;
            }

            if (flags.HasFlag(MediaLoadFlags.AsNoTracking))
            {
                query = query.AsNoTracking();
            }

            if (flags.HasFlag(MediaLoadFlags.WithBlob))
            {
                query = query.Include(x => x.MediaStorage);
            }

            if (flags.HasFlag(MediaLoadFlags.WithFolder))
            {
                query = query.Include(x => x.Folder);
            }

            if (flags.HasFlag(MediaLoadFlags.WithTags))
            {
                query = query.Include(x => x.Tags);
            }

            if (flags.HasFlag(MediaLoadFlags.WithTracks))
            {
                query = query.Include(x => x.Tracks);
            }

            return query;
        }


        private static IQueryable<MediaFile> ApplySearchTerm(IQueryable<MediaFile> query, string term, bool includeAlt, bool exactMatch)
        {
            List<Expression<Func<MediaFile, string>>> expressions = new(2)
            {
                x => x.Name
            };

            if (includeAlt)
            {
                expressions.Add(x => x.Alt);
            }

            if (exactMatch)
            {
                term = "=\"" + term.Trim('"', '\'') + '"';
            }

            return query.ApplySearchFilter(term, Rules.LogicalRuleOperator.Or, expressions.ToArray());
        }
    }
}