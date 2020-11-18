using Smartstore.Domain;

namespace Smartstore.Core.Seo
{
    /// <summary>
    /// Represents an entity which supports slug (SEO friendly one-word URLs)
    /// </summary>
    public interface ISlugSupported : IDisplayedEntity
    {
        // TODO: (core) Override ISlugSupported.GetDisplayName() where applicable (instead of passing "name" to ValidateSlugAsync()).
    }
}
