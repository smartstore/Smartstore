namespace Smartstore.Core.Content.Blocks
{
    /// <summary>
    /// Applies metadata to concrete block types which implement <see cref="IBlock"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class BlockAttribute : Attribute
    {
        public BlockAttribute(string systemName)
        {
            Guard.NotNull(systemName);

            SystemName = systemName;
        }

        /// <summary>
        /// The block system name, e.g. 'html', 'picture' etc.
        /// </summary>
        public string SystemName { get; set; }

        /// <summary>
        /// The english friendly name of the block.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// The icon class name of the block, e.g. 'fa fa-sitemap'.
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// The order of display.
        /// </summary>
        public int DisplayOrder { get; set; }

        public bool IsInternal { get; set; }
    }
}
