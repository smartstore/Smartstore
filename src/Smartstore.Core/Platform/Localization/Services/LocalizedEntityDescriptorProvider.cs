using Autofac;
using FluentMigrator.Infrastructure.Extensions;
using Microsoft.Extensions.Options;
using Smartstore.ComponentModel;
using Smartstore.Core.Configuration;

namespace Smartstore.Core.Localization
{
    public class LocalizedEntityDescriptorProvider : ILocalizedEntityDescriptorProvider
    {
        private readonly List<LocalizedEntityDescriptor> _descriptors = new();
        private readonly List<LoadLocalizedEntityDelegate> _delegates = new();

        public LocalizedEntityDescriptorProvider(ITypeScanner typeScanner, IOptions<LocalizedEntityOptions> options)
        {
            foreach (var type in typeScanner.FindTypes<ILocalizedEntity>())
            {
                if (typeof(ISettings).IsAssignableFrom(type))
                {
                    continue;
                }
                
                var candidateProperties = FastProperty.GetCandidateProperties(type)
                    .Where(x => x.HasAttribute<LocalizedPropertyAttribute>());

                if (candidateProperties.Any())
                {
                    var attr = type.GetAttribute<LocalizedEntityAttribute>(true);

                    _descriptors.Add(new LocalizedEntityDescriptor 
                    {
                        EntityType = type,
                        KeyGroup = attr?.KeyGroup,
                        FilterPredicate = attr?.FilterPredicate,
                        PropertyNames = candidateProperties.Select(p => p.Name).Distinct().ToArray()
                    });
                }
            }

            // ... continue with ISettings

            foreach (var @delegate in options.Value.Delegates)
            {
                _delegates.Add(@delegate);
            }
        }

        public IReadOnlyList<LocalizedEntityDescriptor> GetDescriptors()
        {
            return _descriptors;
        }

        public IReadOnlyList<LoadLocalizedEntityDelegate> GetDelegates()
        {
            return _delegates;
        }
    }
}
