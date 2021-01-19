//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace Smartstore.Core.Content.Blogs
//{
//    public static class BlogPostExtensions
//    {
//        /// <summary>
//        /// Parses <see cref="BlogPost.Tags"/> and returns array of strings.
//        /// </summary>
//        /// <returns>Array of parsed tags.</returns>
//        public static string[] ParseTags(this BlogPost blogPost)
//        {
//            if (blogPost == null)
//                throw new ArgumentNullException(nameof(blogPost));

//            var parsedTags = new List<string>();

//            if (blogPost.Tags.HasValue())
//            {
//                var tags = blogPost.Tags.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

//                foreach (var tag in tags)
//                {
//                    var tmp = tag.Trim();
//                    if (tmp.HasValue())
//                    {
//                        parsedTags.Add(tmp);
//                    }
//                }
//            }

//            return parsedTags.ToArray();
//        }

//        /// <summary>
//        /// Returns all blog posts published between the two dates.
//        /// </summary>
//        /// <returns>Filtered blog posts.</returns>
//        public static IList<BlogPost> GetPostsByDate(this IList<BlogPost> source, DateTime dateFrom, DateTime dateTo)
//        {
//            var list = source.ToList().FindAll(delegate (BlogPost p)
//            {
//                return dateFrom.Date <= p.CreatedOnUtc && p.CreatedOnUtc.Date <= dateTo;
//            });

//            list.TrimExcess();
//            return list;
//        }
//    }
//}
