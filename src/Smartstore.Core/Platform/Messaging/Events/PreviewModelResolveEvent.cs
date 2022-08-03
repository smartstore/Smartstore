namespace Smartstore.Core.Messaging.Events
{
    /// <summary>
    /// Published when preview/test model for given <see cref="ModelName"/> could not be resolved.
    /// This event gives module devs the opportunity to create and provide a preview message model
    /// by assigning a model instance to <see cref="Result"/>.
    /// </summary>
    public class PreviewModelResolveEvent
    {
        /// <summary>
        /// The model/entity name to resolve a preview model for.
        /// </summary>
        public string ModelName { get; init; }

        /// <summary>
        /// The resulting message preview model.
        /// </summary>
        public object Result { get; set; }
    }

}
