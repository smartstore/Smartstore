namespace Smartstore.Web.Api
{
    /// <summary>
    /// Extends <see cref="ConsumesAttribute"/> for Swagger documentation of the request body 
    /// if the OData action has unbound parameters like ODataActionParameters.
    /// Overwrites the wrong Swagger example like <code>"additionalProp1": "string"</code>.
    /// </summary>
    /// <remarks>
    /// Could become obsolete once Swashbuckle can do it.
    /// </remarks>
    public class ApiConsumesAttribute : ConsumesAttribute
    {
        public ApiConsumesAttribute(string contentType, string example = null)
            : this(contentType, example, null)
        {
        }

        public ApiConsumesAttribute(string contentType, string example, params string[] otherContentTypes)
            : base(contentType, otherContentTypes ?? Array.Empty<string>())
        {
            Example = example;
        }

        /// <summary>
        /// A value indicating whether the request body is required.
        /// </summary>
        public bool Required { get; set; } = true;

        /// <summary>
        /// Example request body.
        /// </summary>
        public string Example { get; set; }

        /// <summary>
        /// Schema type of the request body example (if available).
        /// </summary>
        public Type SchemaType { get; set; }
    }
}
