namespace Smartstore.Engine.Modularity
{
    public interface IModuleConstraint
    {
        bool Matches(IModuleDescriptor descriptor, int? storeId);
    }

    internal class NullModuleContraint : IModuleConstraint
    {
        public bool Matches(IModuleDescriptor descriptor, int? storeId)
            => true;
    }
}
