using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Smartstore.News.Domain;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Seo;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.News.Services
{
    internal class NewsItemConverter
    {
        private readonly SmartDbContext _db;
        private readonly ModuleInstallationContext _installContext;
        private readonly SampleMediaUtility _mediaUtility;

        public NewsItemConverter(SmartDbContext db, ModuleInstallationContext installContext)
        {
            _db = Guard.NotNull(db, nameof(db));
            _installContext = Guard.NotNull(installContext, nameof(installContext));
            _mediaUtility = new SampleMediaUtility(db, PathUtility.Combine(installContext.ModuleDescriptor.Path, "App_Data/Samples"));
        }

        /// <summary>
        /// Imports all news xml template files to NewsItem table
        /// </summary>
        /// <returns>List of new imported news posts</returns>
        public async Task<List<NewsItem>> ImportAllAsync()
        {
            var newsImported = new List<NewsItem>();
            var table = _db.Set<NewsItem>();
            var sourceNews = LoadAllAsync();
            var dbNewsMap = (await table.ToListAsync())
                .ToMultimap(x => x.Title, x => x, StringComparer.OrdinalIgnoreCase);

            await foreach (var source in sourceNews)
            {
                if (dbNewsMap.ContainsKey(source.Title))
                {
                    foreach (var target in dbNewsMap[source.Title])
                    {
                        if (source.Title.HasValue()) target.Title = source.Title;
                        if (source.MetaTitle.HasValue()) target.MetaTitle = source.MetaTitle;
                        if (source.MetaDescription.HasValue()) target.MetaDescription = source.MetaDescription;
                        if (source.Short.HasValue()) target.Short = source.Short;
                        if (source.Full.HasValue()) target.Full = source.Full;
                        if (source.CreatedOnUtc > DateTime.MinValue) target.CreatedOnUtc = source.CreatedOnUtc;
                        if (source.MediaFile != null) target.MediaFile = source.MediaFile;
                        if (source.PreviewMediaFile != null) target.PreviewMediaFile = source.PreviewMediaFile;
                        target.AllowComments = source.AllowComments;
                    }
                }
                else
                {
                    var newsItem = new NewsItem();
                    MiniMapper.Map(source, newsItem);
                    newsItem.Published = true;

                    newsImported.Add(newsItem);
                    table.Add(newsItem);
                }
            }

            await _db.SaveChangesAsync();
            return newsImported;
        }

        /// <summary>
        /// Loads all news item from disk.
        /// </summary>
        /// <returns>List of deserialized news posts xml</returns>
        public async IAsyncEnumerable<NewsItem> LoadAllAsync()
        {
            var dir = ResolveNewsDirectory();
            var files = dir.EnumerateFiles("*.xml");

            foreach (var file in files)
            {
                yield return await DeserializeNews(file.PhysicalPath);
            }
        }

        private IDirectory ResolveNewsDirectory()
        {
            var root = _installContext.ApplicationContext.ContentRoot;
            var dir = root.GetDirectory(PathUtility.Combine(_installContext.ModuleDescriptor.Path, "App_Data/Samples"));
            
            // de-DE, de, en
            var testPaths = new List<string>(3) { _installContext.Culture, "en" };
            if (_installContext.Culture.IndexOf('-') > -1)
            {
                testPaths.Insert(1, _installContext.Culture.Substring(0, 2));
            }

            foreach (var path in testPaths.Select(x => PathUtility.Combine(dir.SubPath, x)))
            {
                var subDir = root.GetDirectory(path);
                if (subDir.Exists)
                {
                    return subDir;
                }
            }

            throw new DirectoryNotFoundException($"Could not obtain an news post path for language {_installContext.Culture}. Fallback to 'en' failed, because directory does not exist.");
        }

        private Task<NewsItem> DeserializeNews(string fullPath)
            => DeserializeDocument(XDocument.Load(fullPath));

        private async Task<NewsItem> DeserializeDocument(XDocument doc)
        {
            var result = new NewsItem();
            var nodes = doc.Root.Nodes().OfType<XElement>();

            foreach (var node in nodes)
            {
                var value = node.Value.Trim();

                switch (node.Name.LocalName)
                {
                    case "Title":
                        result.Title = value;
                        break;
                    case "MetaTitle":
                        result.MetaTitle = value;
                        break;
                    case "MetaDescription":
                        result.MetaDescription = value;
                        break;
                    case "Short":
                        result.Short = value;
                        break;
                    case "Full":
                        result.Full = value;
                        break;
                    case "CreatedOn":
                        result.CreatedOnUtc = value.ToDateTime(new DateTime()).Value;
                        break;
                    case "Image":
                        result.MediaFile = await _mediaUtility.CreateMediaFileAsync(value, BuildSlug(Path.GetFileNameWithoutExtension(value)));
                        break;
                    case "ImagePreview":
                        result.PreviewMediaFile = await _mediaUtility.CreateMediaFileAsync(value, BuildSlug(Path.GetFileNameWithoutExtension(value)));
                        break;
                    case "Comments":
                        result.AllowComments = value.ToBool();
                        break;
                }
            }

            return result;
        }

        private static string BuildSlug(string name)
            => SeoHelper.BuildSlug(name, true, false);
    }
}
