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
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Blog.Services
{
    internal class BlogPostConverter
    {
        private readonly SmartDbContext _db;
        private readonly IFileSystem _contentRoot;
        private readonly ModuleInstallationContext _installContext;

        public BlogPostConverter(SmartDbContext db, IFileSystem contentRoot, ModuleInstallationContext installContext)
        {
            _db = Guard.NotNull(db, nameof(db));
            _contentRoot = Guard.NotNull(contentRoot, nameof(contentRoot));
            _installContext = Guard.NotNull(installContext, nameof(installContext));
        }

        /// <summary>
        /// Imports all blog xml files to BlogPost table
        /// </summary>
        /// <param name="rootPath">The root path of blogs to load, e.g. "/Modules/MyModule/BlogPosts". Default is "/App_Data/Samples/blog".</param>
        /// <returns>List of new imported blog posts</returns>
        public async Task<List<BlogPost>> ImportAllAsync(Language language, string rootPath = null)
        {
            // TODO: (core) What is default for rootPath? TBD.
            var blogsImported = new List<BlogPost>();
            var table = _db.Set<BlogPost>();
            var sourceBlogs = LoadAll(language, rootPath);
            var dbBlogMap = (await table.ToListAsync())
                .ToMultimap(x => x.Title, x => x, StringComparer.OrdinalIgnoreCase);

            foreach (var source in sourceBlogs)
            {
                if (dbBlogMap.ContainsKey(source.Title))
                {
                    foreach (var target in dbBlogMap[source.Title])
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
                }
            }

            await _db.SaveChangesAsync();
            return blogsImported;
        }

        /// <summary>
        /// Loads all blog post from disk.
        /// </summary>
        /// <param name="language">Language</param>
        /// <param name="rootPath">The root path of blogs to load, e.g. "/Modules/MyModule/BlogPosts". Default is "/App_Data/Samples/blog".</param>
        /// <returns>List of deserialized blog posts xml</returns>
        public IEnumerable<BlogPost> LoadAll(Language language, string rootPath = null)
        {
            // TODO: (core) What is default for rootPath? TBD.
            Guard.NotNull(language, nameof(language));

            var dir = ResolveBlogDirectory(language, rootPath);
            var files = dir.EnumerateFiles("*.xml");

            foreach (var file in files)
            {
                yield return DeserializeBlog(file.PhysicalPath);
            }
        }

        private IDirectory ResolveBlogDirectory(Language language, string rootPath = null)
        {
            var testPaths = new[]
            {
                language.LanguageCulture,
                language.GetTwoLetterISOLanguageName(),
                "en"
            };

            // TODO: (core) Finish
            //var rootPath = CommonHelper.MapPath(virtualRootPath.NullEmpty() ?? "~/App_Data/Samples/blog/");
            //foreach (var path in testPaths.Select(x => Path.Combine(rootPath, x)))
            //{
            //    if (Directory.Exists(path))
            //    {
            //        return new DirectoryInfo(path);
            //    }
            //}

            throw new DirectoryNotFoundException($"Could not obtain an blog post path for language {language.LanguageCulture}. Fallback to 'en' failed, because directory does not exist.");
        }

        private BlogPost DeserializeBlog(string fullPath)
        {
            return DeserializeDocument(XDocument.Load(fullPath));
        }

        private BlogPost DeserializeDocument(XDocument doc)
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
                        result.MediaFile = CreateImage(value, BuildSlug(Path.GetFileNameWithoutExtension(value)));
                        break;
                    case "ImagePreview":
                        result.PreviewMediaFile = CreateImage(value, BuildSlug(Path.GetFileNameWithoutExtension(value)));
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

        private MediaFile CreateImage(string fileName, string seoFilename = null)
        {
            // TODO: (core) Finish this shit
            return null;
            
            //try
            //{
            //    var ext = Path.GetExtension(fileName);
            //    var mimeType = MimeTypes.MapNameToMimeType(ext);
            //    var path = Path.Combine(CommonHelper.MapPath("~/App_Data/Samples/blog/"), fileName).Replace('/', '\\');
            //    var buffer = File.ReadAllBytes(path);
            //    var now = DateTime.UtcNow;

            //    var name = seoFilename.HasValue()
            //        ? seoFilename.Truncate(100) + ext
            //        : Path.GetFileName(fileName).ToLower().Replace('_', '-');

            //    var file = new MediaFile
            //    {
            //        Name = name,
            //        MediaType = "image",
            //        MimeType = mimeType,
            //        Extension = ext.EmptyNull().TrimStart('.'),
            //        CreatedOnUtc = now,
            //        UpdatedOnUtc = now,
            //        Size = buffer.Length,
            //        MediaStorage = new MediaStorage { Data = buffer },
            //        Version = 1 // so that FolderId is set later during track detection
            //    };

            //    return file;
            //}
            //catch (Exception ex)
            //{
            //    // Throw ex;
            //    System.Diagnostics.Debug.WriteLine(ex.Message);
            //    return null;
            //}
        }

        private static string BuildSlug(string name)
            => SeoHelper.BuildSlug(name, true, false);
    }
}
