using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Net.Http.Headers;

namespace Smartstore.Web.Controllers
{
    /// <summary>
    /// Represents an <see cref="ActionResult"/> that when executed will
    /// write a binary file to the response. The buffer is resolved
    /// only when the precondition state is not 304.
    /// </summary>
    public class LazyFileContentResult : FileResult
    {
        private Func<Task<byte[]>> _bufferAccessor;

        /// <summary>
        /// Creates a new <see cref="LazyFileContentResult"/> instance with
        /// the provided <paramref name="bufferAccessor"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="bufferAccessor">The files bytes accessor.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public LazyFileContentResult(Func<byte[]> bufferAccessor, string contentType, long? fileLength = null)
            : this(() => Task.FromResult(bufferAccessor()), MediaTypeHeaderValue.Parse(contentType), fileLength)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LazyFileContentResult"/> instance with
        /// the provided <paramref name="bufferAccessor"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="bufferAccessor">The files bytes accessor.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public LazyFileContentResult(Func<Task<byte[]>> bufferAccessor, string contentType, long? fileLength = null)
            : this(bufferAccessor, MediaTypeHeaderValue.Parse(contentType), fileLength)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LazyFileContentResult"/> instance with
        /// the provided <paramref name="bufferAccessor"/> and the
        /// provided <paramref name="contentType"/>.
        /// </summary>
        /// <param name="bufferAccessor">The files bytes accessor.</param>
        /// <param name="contentType">The Content-Type header of the response.</param>
        public LazyFileContentResult(Func<Task<byte[]>> bufferAccessor, MediaTypeHeaderValue contentType, long? fileLength = null)
            : base(contentType.ToString())
        {
            BufferAccessor = Guard.NotNull(bufferAccessor, nameof(bufferAccessor));
            FileLength = fileLength;
        }

        /// <summary>
        /// Gets or sets the file contents.
        /// </summary>
        public Func<Task<byte[]>> BufferAccessor
        {
            get => _bufferAccessor;
            set => _bufferAccessor = Guard.NotNull(value, nameof(value));
        }

        /// <summary>
        /// Gets or sets the file length.
        /// </summary>
        public long? FileLength { get; set; }

        /// <inheritdoc />
        public override Task ExecuteResultAsync(ActionContext context)
        {
            Guard.NotNull(context, nameof(context));

            var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<LazyFileContentResult>>();
            return executor.ExecuteAsync(context, this);
        }
    }
}
