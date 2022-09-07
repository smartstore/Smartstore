namespace Smartstore.Core.Localization
{
    public class LocalizedEntityDescriptorProvider : ILocalizedEntityDescriptorProvider
    {
        private readonly List<LocalizedEntityDescriptor> _descriptors = new();

        public LocalizedEntityDescriptorProvider(ITypeScanner typeScanner)
        {
            foreach (var type in typeScanner.FindTypes<ILocalizedEntity>())
            {
                if (type.TryGetAttribute<LocalizedEntityAttribute>(true, out var attr))
                {
                    _descriptors.Add(attr.ToDescriptor(type));
                }
            }
        }

        public IReadOnlyList<LocalizedEntityDescriptor> GetEntityDescriptors()
        {
            return _descriptors;
        }
    }
}
