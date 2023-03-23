using System.Text.RegularExpressions;
using Smartstore.Collections;
using Smartstore.Core.Data;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Core.Content.Media
{
    public partial class ImageOffloader : IImageOffloder
    {
        public class EntityStub
        {
            public int Id { get; set; }
            public string Html { get; set; }
        }

        private enum TypeCase
        {
            Product,
            Category,
            Brand,
            Topic
        }
        
        //[GeneratedRegex(@"(?<=<img[^>]*src\s*=\s*['""])data:image\/(?<format>[a-z]+);base64,(?<data>[^'""]+)(?=['""][^>]*>)")]
        [GeneratedRegex(@"src\s*=\s*['""](data:image\/(?<format>[a-z]+);base64,(?<data>[^'""]+))['""]")]
        private static partial Regex EmbeddedImagesRegex();
        private static readonly Regex _rgEmbeddedImages = EmbeddedImagesRegex();

        private const string Base64Fragment = "src=\"data:image/";
        private const string DefaultMediaFolder = "file/extracted";

        private readonly SmartDbContext _db;
        private readonly IMediaService _mediaService;
        private readonly IFolderService _folderService;

        public ImageOffloader(SmartDbContext db, IMediaService mediaService, IFolderService folderService)
        {
            _db = db;
            _mediaService = mediaService;
            _folderService = folderService;
        }

        public bool HasEmbeddedImage(string html)
        {
            if (html.IsEmpty())
            {
                return false;
            }

            return html.Contains(Base64Fragment);
        }

        public async Task<TreeNode<MediaFolderNode>> GetDefaultMediaFolderAsync()
        {
            var node = _folderService.GetNodeByPath(DefaultMediaFolder);
            if (node == null)
            {
                var folder = await _mediaService.CreateFolderAsync(DefaultMediaFolder);
                node = folder.Node;
            }

            return node;
        }

        public async Task<OffloadImagesBatchResult> BatchOffloadEmbeddedImagesAsync(int take = 200)
        {
            Guard.IsPositive(take);

            var destinationFolder = await GetDefaultMediaFolderAsync();

            var (numAffectedEntities, affectedIdGroups) = await GetAffectedEntityIdsGroupedAsync(take);
            if (affectedIdGroups.TotalValueCount > 0)
            {
                _mediaService.ImagePostProcessingEnabled = false;
            }

            var numProcessedEntities = 0;
            var numAttempted = 0;
            var numFailed = 0;

            foreach (var grp in affectedIdGroups)
            {
                var typeCase = grp.Key;
                var ids = grp.Value;

                Func<int[], List<EntityStub>> loader;
                Action<int, string> saver;
                Func<int, string> entityTagBuilder;

                switch (typeCase)
                {
                    case TypeCase.Topic:
                        loader = chunk => ApplyFilter(_db.Topics, chunk).Select(x => new EntityStub { Id = x.Id, Html = x.Body }).ToList();
                        saver = (id, html) => ApplyFilter(_db.Topics, id).ExecuteUpdate(x => x.SetProperty(p => p.Body, p => html));
                        entityTagBuilder = id => "t" + id.ToStringInvariant();
                        break;
                    case TypeCase.Category: 
                        loader = chunk => ApplyFilter(_db.Categories, chunk).Select(x => new EntityStub { Id = x.Id, Html = x.Description }).ToList();
                        saver = (id, html) => ApplyFilter(_db.Categories, id).ExecuteUpdate(x => x.SetProperty(p => p.Description, p => html));
                        entityTagBuilder = id => "c" + id.ToStringInvariant();
                        break;
                    case TypeCase.Brand:
                        loader = chunk => ApplyFilter(_db.Manufacturers, chunk).Select(x => new EntityStub { Id = x.Id, Html = x.Description }).ToList();
                        saver = (id, html) => ApplyFilter(_db.Manufacturers, id).ExecuteUpdate(x => x.SetProperty(p => p.Description, p => html));
                        entityTagBuilder = id => "m" + id.ToStringInvariant();
                        break;
                    default:
                        loader = chunk => ApplyFilter(_db.Products, chunk).Select(x => new EntityStub { Id = x.Id, Html = x.FullDescription }).ToList();
                        saver = (id, html) => ApplyFilter(_db.Products, id).ExecuteUpdate(x => x.SetProperty(p => p.FullDescription, p => html));
                        entityTagBuilder = id => "p" + id.ToStringInvariant();
                        break;
                }

                foreach (var chunk in ids.Chunk(10))
                {
                    var stubs = loader(chunk);

                    foreach (var stub in stubs)
                    {
                        numProcessedEntities++;

                        var offloadResult = await OffloadEmbeddedImagesAsync(
                            stub.Html,
                            destinationFolder.Value,
                            entityTagBuilder(stub.Id));

                        numAttempted += offloadResult.NumAttempted;
                        numFailed += offloadResult.NumFailed;

                        if (offloadResult.NumAttempted > 0 && !string.IsNullOrEmpty(offloadResult.ResultHtml))
                        {
                            saver(stub.Id, offloadResult.ResultHtml);
                        }
                    }
                }
            }

            return new OffloadImagesBatchResult
            {
                NumAffectedEntities = numAffectedEntities,
                NumProcessedEntities = numProcessedEntities,
                NumAttempted = numAttempted,
                NumFailed = numFailed
            };
        }

        public async Task<OffloadImageResult> OffloadEmbeddedImagesAsync(string html, MediaFolderNode destinationFolder, string entityTag)
        {
            Guard.NotEmpty(html);
            Guard.NotNull(destinationFolder);
            Guard.NotEmpty(entityTag);

            var result = new OffloadImageResult();

            var resultHtml = await _rgEmbeddedImages.ReplaceAsync(html, async match =>
            {
                result.NumAttempted++;

                var format = match.Groups["format"].Value;
                var data = match.Groups["data"].Value;
                if (format == "jpeg")
                {
                    format = "jpg";
                }

                string fragment = null;

                try
                {
                    using var stream = new MemoryStream(Convert.FromBase64String(data));

                    //var fileName = $"p{p.Id.ToStringInvariant()}-{CommonHelper.GenerateRandomDigitCode(8)}.{format}";
                    var fileName = $"{entityTag}-{CommonHelper.GenerateRandomDigitCode(8)}.{format}";
                    var filePath = PathUtility.Join(destinationFolder.Path, fileName);
                    var fileInfo = await _mediaService.SaveFileAsync(filePath, stream, false);

                    if (fileInfo?.File?.IsTransientRecord() == true)
                    {
                        fragment = match.Value;
                    }
                    else
                    {
                        result.OffloadedFiles.Add(fileInfo);
                        fragment = fileInfo.Url;
                    }
                }
                catch
                {
                    fragment = match.Value;
                }

                return $"src=\"{fragment}\"";
            });

            if (result.OffloadedFiles.Count > 0)
            {
                result.ResultHtml = resultHtml;
            }

            return result;
        }

        private async Task<(int, Multimap<TypeCase, int>)> GetAffectedEntityIdsGroupedAsync(int take)
        {
            int took = 0;
            var map = new Multimap<TypeCase, int>();

            var q1 = _db.Topics.IgnoreQueryFilters().Where(x => x.Body.Contains(Base64Fragment)).OrderBy(x => x.Id).Select(x => x.Id);
            var ids = await q1.ToPagedList(0, take).LoadAsync();
            var count = await q1.CountAsync();
            var totalCount = count;
            var col = (ICollection<int>)null;
            if (count > 0)
            {
                col = map[TypeCase.Topic];
                col.AddRange(ids);
                took += col.Count;
            }

            var q2 = _db.Categories.IgnoreQueryFilters().Where(x => x.Description.Contains(Base64Fragment)).OrderBy(x => x.Id).Select(x => x.Id);
            count += await q2.CountAsync();
            totalCount += count;
            if (count > 0 && took < take)
            {
                ids = await q2.ToPagedList(0, take - took).LoadAsync();
                col = map[TypeCase.Category];
                col.AddRange(ids);
                took += col.Count;
            }

            var q3 = _db.Manufacturers.IgnoreQueryFilters().Where(x => x.Description.Contains(Base64Fragment)).OrderBy(x => x.Id).Select(x => x.Id);
            count += await q3.CountAsync();
            totalCount += count;
            if (count > 0 && took < take)
            {
                ids = await q3.ToPagedList(0, take - took).LoadAsync();
                col = map[TypeCase.Brand];
                col.AddRange(ids);
                took += col.Count;
            }

            var q4 = _db.Products.IgnoreQueryFilters().Where(x => x.FullDescription.Contains(Base64Fragment)).OrderBy(x => x.Id).Select(x => x.Id);
            count += await q4.CountAsync();
            totalCount += count;
            if (count > 0 && took < take)
            {
                ids = await q4.ToPagedList(0, take - took).LoadAsync();
                col = map[TypeCase.Product];
                col.AddRange(ids);
                took += col.Count;
            }

            return (totalCount, map);
        }

        private static IQueryable<T> ApplyFilter<T>(IQueryable<T> query, int[] ids) where T : BaseEntity
        {
            return query.IgnoreQueryFilters().Where(x => ids.Contains(x.Id));
        }

        private static IQueryable<T> ApplyFilter<T>(IQueryable<T> query, int id) where T : BaseEntity
        {
            return query.IgnoreQueryFilters().Where(x => x.Id == id);
        }
    }
}
