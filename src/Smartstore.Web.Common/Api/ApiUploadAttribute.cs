namespace Smartstore.Web.Api
{
    /// <summary>
    /// Adds a file uploader to Swagger documentation.
    /// </summary>
    /// <remarks>
    /// https://stackoverflow.com/questions/64804243/how-to-enable-documentation-and-file-upload-for-formdata-parameter-in-swashbuckl
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ApiUploadAttribute : ConsumesAttribute
    {
        public ApiUploadAttribute()
            : this("multipart/form-data")
        {
        }

        public ApiUploadAttribute(string contentType, params string[] otherContentTypes)
            : base(contentType, otherContentTypes)
        {
        }

        public string PropertyName { get; set; } = "file";
    }
}
