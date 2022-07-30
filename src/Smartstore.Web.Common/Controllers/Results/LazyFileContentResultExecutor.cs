using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Smartstore.Web.Controllers
{
    public class LazyFileContentResultExecutor : FileResultExecutorBase, IActionResultExecutor<LazyFileContentResult>
    {
        public LazyFileContentResultExecutor(ILoggerFactory loggerFactory)
            : base(CreateLogger<FileContentResultExecutor>(loggerFactory))
        {
        }

        public async Task ExecuteAsync(ActionContext context, LazyFileContentResult result)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(result, nameof(result));

            var (range, rangeLength, serveBody) = SetHeadersAndLog(
                context,
                result,
                result.FileLength,
                result.EnableRangeProcessing,
                result.LastModified,
                result.EntityTag);

            if (serveBody)
            {
                var buffer = await result.BufferAccessor();
                var innerResult = new FileContentResult(buffer, result.ContentType)
                {
                    EnableRangeProcessing = result.EnableRangeProcessing,
                    EntityTag = result.EntityTag,
                    FileDownloadName = result.FileDownloadName,
                    LastModified = result.LastModified
                };

                var innerExecutor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<FileContentResult>>();

                await innerExecutor.ExecuteAsync(context, innerResult);
            }
        }
    }
}
