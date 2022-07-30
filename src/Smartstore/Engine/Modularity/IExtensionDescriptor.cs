namespace Smartstore.Engine.Modularity
{
    public enum ExtensionType
    {
        Module,
        Theme
    }

    public interface IExtensionDescriptor
    {
        /// <summary>
        /// (System) name of extension. Also reflects the directory name under <see cref="Path"/>.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Type of extension (module or theme)
        /// </summary>
        ExtensionType ExtensionType { get; }

        /// <summary>
        /// Optional friendly name of extension
        /// </summary>
        string FriendlyName { get; }

        /// <summary>
        /// Optional description of extension
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Optional group identifier of extension
        /// </summary>
        string Group { get; }

        /// <summary>
        /// Optional author of extension
        /// </summary>
        string Author { get; }

        /// <summary>
        /// Optional project web site / url of extension
        /// </summary>
        string ProjectUrl { get; }

        /// <summary>
        /// Optional tags of extension
        /// </summary>
        string Tags { get; }

        /// <summary>
        /// The current version of extension
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// The minimum compatible application version.
        /// </summary>
        Version MinAppVersion { get; }
    }
}
