using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Smartstore.Blog.Domain;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Seo;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Blog.Services
{
    internal class BlogPostConverter
    {
        private readonly SmartDbContext _db;
        private readonly ModuleInstallationContext _installContext;
        private readonly SampleMediaUtility _mediaUtility;

        public BlogPostConverter(SmartDbContext db, ModuleInstallationContext installContext)
        {
            _db = Guard.NotNull(db, nameof(db));
            _installContext = Guard.NotNull(installContext, nameof(installContext));
            _mediaUtility = new SampleMediaUtility(db, PathUtility.Combine(installContext.ModuleDescriptor.Path, "App_Data/Samples"));
        }

        /// <summary>
        /// Imports all blog xml template files to BlogPost table
        /// </summary>
        /// <returns>List of new imported blog posts</returns>
        public async Task<List<BlogPost>> ImportAllAsync()
        {
            var blogsImported = new List<BlogPost>();
            var table = _db.Set<BlogPost>();
            var sourceBlogs = LoadAllAsync();
            var existingBlogs = (await table.ToListAsync())
                .ToMultimap(x => x.Title, x => x, StringComparer.OrdinalIgnoreCase);

            await foreach (var source in sourceBlogs)
            {
                if (existingBlogs.ContainsKey(source.Title))
                {
                    foreach (var target in existingBlogs[source.Title])
                    {
                        if (source.Title.HasValue()) target.Title = source.Title;
                        if (source.MetaTitle.HasValue()) target.MetaTitle = source.MetaTitle;
                        if (source.MetaDescription.HasValue()) target.MetaDescription = source.MetaDescription;
                        if (source.Intro.HasValue()) target.Intro = source.Intro;
                        if (source.Body.HasValue()) target.Body = source.Body;
                        if (source.Tags.HasValue()) target.Tags = source.Tags;
                        if (source.CreatedOnUtc > DateTime.MinValue) target.CreatedOnUtc = source.CreatedOnUtc;
                        if (source.MediaFile != null) target.MediaFile = source.MediaFile;
                        if (source.PreviewMediaFile != null) target.PreviewMediaFile = source.PreviewMediaFile;
                        if (source.SectionBg.HasValue()) target.SectionBg = source.SectionBg;
                        target.DisplayTagsInPreview = source.DisplayTagsInPreview;
                        target.PreviewDisplayType = source.PreviewDisplayType;
                        target.AllowComments = source.AllowComments;
                    }
                }
                else
                {
                    var blogPost = new BlogPost();
                    MiniMapper.Map(source, blogPost);
                    blogPost.IsPublished = true;

                    blogsImported.Add(blogPost);
                    table.Add(blogPost);

                    // INFO: save immediately to avoid MySqlException "Error submitting ...MB packet; ensure 'max_allowed_packet' is greater than ...MB".
                    await _db.SaveChangesAsync();
                }
            }

            await _db.SaveChangesAsync();

            return blogsImported;
        }

        /// <summary>
        /// Loads all blog post from disk.
        /// </summary>
        /// <returns>List of deserialized blog posts xml</returns>
        public async IAsyncEnumerable<BlogPost> LoadAllAsync()
        {
            var dir = ResolveBlogDirectory();
            var files = dir.EnumerateFiles("*.xml");

            foreach (var file in files)
            {
                yield return await DeserializeBlog(file.PhysicalPath);
            }
        }

        private IDirectory ResolveBlogDirectory()
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

            throw new DirectoryNotFoundException($"Could not obtain an blog post path for language {_installContext.Culture}. Fallback to 'en' failed, because directory does not exist.");
        }

        private Task<BlogPost> DeserializeBlog(string fullPath)
            => DeserializeDocument(XDocument.Load(fullPath));

        private async Task<BlogPost> DeserializeDocument(XDocument doc)
        {
            var result = new BlogPost();
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
                    case "Intro":
                        result.Intro = value;
                        break;
                    case "Body":
                        result.Body = value;
                        break;
                    case "Tags":
                        result.Tags = value;
                        break;
                    case "DisplayTags":
                        result.DisplayTagsInPreview = value.ToBool();
                        break;
                    case "CreatedOn":
                        result.CreatedOnUtc = value.ToDateTime(new DateTime()).Value;
                        break;
                    case "DisplayType":
                        result.PreviewDisplayType = (PreviewDisplayType)int.Parse(value);
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
                    case "SectionBg":
                        result.SectionBg = value;
                        break;
                }
            }

            return result;
        }

        private static string BuildSlug(string name)
            => SeoHelper.BuildSlug(name, true, false);
    }
}
