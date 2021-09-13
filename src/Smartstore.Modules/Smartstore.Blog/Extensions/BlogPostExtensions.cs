using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Blog.Domain;
using Smartstore.Core.Seo;

namespace Smartstore.Blog
{
    public static class BlogPostExtensions
    {
        /// <summary>
        /// Parses <see cref="BlogPost.Tags"/> and returns array of strings.
        /// </summary>
        /// <returns>Array of parsed tags.</returns>
        public static string[] ParseTags(this BlogPost blogPost)
            => Guard.NotNull(blogPost, nameof(blogPost)).Tags.SplitSafe(',').ToArray();

        /// <summary>
        /// Returns all blog posts published between the two dates.
        /// </summary>
        /// <returns>Filtered blog posts.</returns>
        public static IList<BlogPost> GetPostsByDate(this IList<BlogPost> source, DateTime dateFrom, DateTime dateTo)
        {
            var list = source.ToList().FindAll(delegate (BlogPost p)
            {
                return dateFrom.Date <= p.CreatedOnUtc && p.CreatedOnUtc.Date <= dateTo;
            });

            list.TrimExcess();
            return list;
        }

        /// <summary>
        /// Filter a list by <see cref="BlogPost.Tags"/>.
        /// </summary>
        public static IEnumerable<BlogPost> FilterByTag(this IList<BlogPost> posts, string tag)
        {
            if (tag == null || !tag.HasValue())
            {
                return posts;
            }

            tag = tag.Trim();

            var taggedBlogPosts = new List<BlogPost>();

            foreach (var blogPost in posts)
            {
                var tags = blogPost.ParseTags().Select(x => SeoHelper.BuildSlug(x));

                if (tags.FirstOrDefault(t => t.EqualsNoCase(tag)).HasValue())
                {
                    taggedBlogPosts.Add(blogPost);
                }
            }

            return taggedBlogPosts;
        }
    }
}
