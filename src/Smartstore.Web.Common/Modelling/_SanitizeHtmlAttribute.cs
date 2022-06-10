namespace Smartstore.Web.Modelling
{
    // TODO: (core) Finish implementation and usage of SanitizeHtmlAttribute

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SanitizeHtmlAttribute : Attribute
    {
        public bool IsFragment
        {
            get;
            set;
        } = true;

        //public void OnMetadataCreated(ModelMetadata metadata)
        //{
        //    Guard.NotNull(metadata, nameof(metadata));

        //    metadata.RequestValidationEnabled = false;
        //}
    }
}
