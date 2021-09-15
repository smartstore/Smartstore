using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Blog.Domain;
using Smartstore.Core.Data;
using Smartstore.Core.Messaging.Events;
using Smartstore.Events;
using Smartstore.Utilities;

namespace Smartstore.Blog
{
    public class Events : IConsumer
    {
        private readonly SmartDbContext _db;

        public Events(SmartDbContext db)
        {
            _db = db;
        }

        public async Task HandleEventAsync(PreviewModelResolveEvent message)
        {
            if (message.ModelName == "BlogComment")
            {
                message.Result = await GetRandomEntity();
            }
        }

        private async Task<BlogComment> GetRandomEntity()
        {
            var query = _db.BlogComments().AsNoTracking();
            var count = await query.CountAsync();

            BlogComment result;

            if (count > 0)
            {
                // Fetch a random blog comment.
                var skip = CommonHelper.GenerateRandomInteger(0, count);
                result = await query.OrderBy(x => x.Id).Skip(skip).FirstOrDefaultAsync();
            }
            else
            {
                result = new BlogComment { 
                    CommentText = @"Lorem ipsum dolor sit amet, consetetur sadipscing elitr, 
                                    sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua."
                };
            }

            return result;
        }
    }
}
