using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Blog.Domain;

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
    }
}
