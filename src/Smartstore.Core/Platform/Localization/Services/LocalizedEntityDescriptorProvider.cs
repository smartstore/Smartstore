using FluentMigrator.Infrastructure.Extensions;
using Smartstore.ComponentModel;

namespace Smartstore.Core.Localization
{
    public class LocalizedEntityDescriptorProvider : ILocalizedEntityDescriptorProvider
    {
        private readonly List<LocalizedEntityDescriptor> _descriptors = new();

        public LocalizedEntityDescriptorProvider(ITypeScanner typeScanner)
        {
            foreach (var type in typeScanner.FindTypes<ILocalizedEntity>())
            {
                var candidateProperties = FastProperty.GetCandidateProperties(type)
                    .Where(x => x.HasAttribute<LocalizedPropertyAttribute>());

                if (candidateProperties.Any())
                {
                    _descriptors.Add(new LocalizedEntityDescriptor 
                    {
                        EntityType = type,
                        FilterPredicate = type.GetAttribute<LocalizedEntityAttribute>(true)?.FilterPredicate,
                        PropertyNames = candidateProperties.Select(p => p.Name).Distinct().ToArray()
                    });
                }
            }
        }

        public IReadOnlyList<LocalizedEntityDescriptor> GetEntityDescriptors()
        {
            return _descriptors;
        }
    }
}
