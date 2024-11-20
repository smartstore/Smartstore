using Smartstore.Core.Rules;

namespace Smartstore.Core.Catalog.Rules
{
    public class ProductRuleDescriptorCollection : RuleDescriptorCollection
    {
        private bool _variantsLoaded;
        private bool _attributesLoaded;

        private readonly WeakReference<ProductRuleProvider> _provider;

        public ProductRuleDescriptorCollection(ProductRuleProvider provider, IEnumerable<RuleDescriptor> descriptors)
            : base(descriptors)
        {
            _provider = new WeakReference<ProductRuleProvider>(provider);
        }

        public override RuleDescriptor FindDescriptor(string name)
        {
            if (!_variantsLoaded && name.StartsWith(ProductRuleProvider.VariantPrefix))
            {
                LoadDescriptors(p => p.LoadVariantDescriptors());
                _variantsLoaded = true;
            }

            if (!_attributesLoaded && name.StartsWith(ProductRuleProvider.AttributePrefix))
            {
                LoadDescriptors(p => p.LoadAttributeDescriptors());
                _attributesLoaded = true;
            }

            return base.FindDescriptor(name);
        }

        public override IEnumerator<RuleDescriptor> GetEnumerator()
        {
            // Iterate over existing elements in the collection
            foreach (var descriptor in _innerCollection)
            {
                yield return descriptor;
            }

            // Load variant descriptors if not already loaded
            if (!_variantsLoaded)
            {
                var descriptors = LoadDescriptors(p => p.LoadVariantDescriptors());
                _variantsLoaded = true;

                // Iterate only over newly added elements
                foreach (var descriptor in descriptors)
                {
                    yield return descriptor;
                }
            }

            // Load attribute descriptors if not already loaded
            if (!_attributesLoaded)
            {
                var descriptors = LoadDescriptors(p => p.LoadAttributeDescriptors());
                _attributesLoaded = true;

                // Iterate only over newly added elements
                foreach (var descriptor in descriptors)
                {
                    yield return descriptor;
                }
            }
        }

        private IEnumerable<RuleDescriptor> LoadDescriptors(Func<ProductRuleProvider, IEnumerable<RuleDescriptor>> loader)
        {
            if (_provider.TryGetTarget(out var provider))
            {
                var descriptors = loader(provider);
                descriptors.Each(Add);
                return descriptors;
            }

            return new List<RuleDescriptor>();
        }
    }
}
