using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Content.Blocks
{
    /// <summary>
    /// Represents block registration metadata.
    /// </summary>
    public interface IBlockMetadata : IProviderMetadata
    {
        string ModuleName { get; }
        string Icon { get; }
        bool IsInternal { get; }
        bool IsInbuilt { get; }
        Type BlockClrType { get; }
        Type BlockHandlerClrType { get; }
    }

    public class BlockMetadata : IBlockMetadata, ICloneable<BlockMetadata>
    {
        public string ModuleName { get; set; }
        public string SystemName { get; set; }
        public string FriendlyName { get; set; }
        public string Description { get; set; }
        public string ResourceKeyPattern { get; set; }
        public string Icon { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsInternal { get; set; }
        public bool IsInbuilt { get; set; }
        public Type BlockClrType { get; set; }
        public Type BlockHandlerClrType { get; set; }

        public BlockMetadata Clone()
            => (BlockMetadata)MemberwiseClone();

        object ICloneable.Clone()
            => MemberwiseClone();
    }
}
