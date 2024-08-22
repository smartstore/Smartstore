#nullable enable

using Microsoft.OData;

namespace Smartstore.Web.Api
{
    public static partial class ODataHelper
    {
        /// <summary>
        /// Creates an OData error.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="statusCode">E.g. the current HTTP status.</param>
        /// <param name="details">Optional details of the error.</param>
        public static ODataError CreateError(
            string message,
            int? statusCode = null,
            Exception? innerException = null,
            ICollection<ODataErrorDetail>? details = null)
        {
            Guard.NotEmpty(message);

            var error = new ODataError
            {
                Message = message,
                Code = statusCode?.ToString(),
                Details = details
            };

            if (innerException != null)
            {
                error.InnerError = new ODataInnerError(new Dictionary<string, ODataValue>
                {
                    ["message"] = innerException.Message.HasValue() ? new ODataPrimitiveValue(innerException.Message) : new ODataNullValue(),
                    ["type"] = new ODataPrimitiveValue(innerException.GetType().FullName),
                    ["stacktrace"] = innerException.StackTrace.HasValue() ? new ODataPrimitiveValue(innerException.StackTrace) : new ODataNullValue()
                });
            }

            return error;
        }
    }
}
