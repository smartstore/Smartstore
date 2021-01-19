using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using Autofac;
using Smartstore.Core.Content.Blogs;
using Smartstore.Core.Content.Seo;
using Smartstore.Core.Stores;
using Smartstore.Engine;

namespace Smartstore
{
    public static partial class BlogPostQueryExtensions
    {
        /// <summary>
        /// Applies standard filter and sorts descending by <see cref="BlogPost.CreatedOnUtc"/>.
        /// </summary>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <param name="dateFrom">Applies lower limit date time filter by <see cref="BlogPost.CreatedOnUtc"/>.</param>
        /// <param name="dateTo">Applies upper limit date time filter by <see cref="BlogPost.CreatedOnUtc"/>.</param>
        /// <param name="languageId">Language identifier to apply filter by <see cref="Language.Id"/>.</param>
        /// <param name="includeHidden">Applies filter by <see cref="BlogPost.Published"/>.</param>
        /// <param name="maxAge">Applies maximum age date time filter by <see cref="BlogPost.CreatedOnUtc"/>.</param>
        /// <param name="title">Applies filter by <see cref="BlogPost.Title"/>.</param>
        /// <param name="intro">Applies filter by <see cref="BlogPost.Intro"/>.</param>
        /// <param name="body">Applies filter by <see cref="BlogPost.Body"/>.</param>
        /// <returns>Ordered blog post query.</returns>
        public static IOrderedQueryable<BlogPost> ApplyStandardFilter(
            this IQueryable<BlogPost> query,
            int storeId,
            DateTime? dateFrom,
            DateTime? dateTo,
            int languageId = 0,
            bool includeHidden = false,
            DateTime? maxAge = null,
            string title = "",
            string intro = "",
            string body = "")
        {
            Guard.NotNull(query, nameof(query));

            if (dateFrom.HasValue)
            {
                query = query.Where(b => dateFrom.Value <= b.CreatedOnUtc);
            }
            
            if (dateTo.HasValue)
            {
                query = query.Where(b => dateTo.Value >= b.CreatedOnUtc);
            }
            
            if (maxAge.HasValue)
            {
                query = query.Where(b => b.CreatedOnUtc >= maxAge.Value);
            }
            
            if (title.HasValue())
            {
                query = query.Where(b => b.Title.Contains(title));
            }
            
            if (intro.HasValue())
            {
                query = query.Where(b => b.Intro.Contains(intro));
            }
            
            if (body.HasValue())
            {
                query = query.Where(b => b.Body.Contains(body));
            }
            
            if (languageId != 0)
            {
                query = query.Where(b => !b.LanguageId.HasValue || b.LanguageId == languageId);
            }

            if (!includeHidden)
            {
                var utcNow = DateTime.UtcNow;
                query = query.Where(b => !b.StartDateUtc.HasValue || b.StartDateUtc <= utcNow);
                query = query.Where(b => !b.EndDateUtc.HasValue || b.EndDateUtc >= utcNow);
                query = query.Where(b => b.IsPublished);
            }

            if (storeId > 0)
            {
                query = query.ApplyStoreFilter(storeId);
            }

            return query.OrderByDescending(x => x.CreatedOnUtc);
        }

        /// <summary>
        /// Applies filter by <see cref="BlogPost.Tags"/>.
        /// </summary>
        public static IQueryable<BlogPost> ApplyTagFilter(this IQueryable<BlogPost> query, string tag)
        {
            tag = tag.Trim();

            var seoSettings = EngineContext.Current.Application.Services.Resolve<SeoSettings>();
            var taggedBlogPosts = new List<BlogPost>();

            foreach (var blogPost in query)
            {
                var tags = blogPost.ParseTags().Select(x => SeoHelper.BuildSlug(x,
                    seoSettings.ConvertNonWesternChars,
                    seoSettings.AllowUnicodeCharsInUrls,
                    true,
                    seoSettings.SeoNameCharConversion));

                if (tags.FirstOrDefault(t => t.Equals(tag, StringComparison.InvariantCultureIgnoreCase)).HasValue())
                {
                    taggedBlogPosts.Add(blogPost);
                }
            }

            return taggedBlogPosts.AsQueryable();
        }
    }
}
