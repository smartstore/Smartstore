using Smartstore.Domain;

namespace Smartstore.Core.Content.Seo
{
    /// <summary>
    /// Represents an entity which supports slug (SEO friendly one-word URLs)
    /// </summary>
    public interface ISlugSupported : IDisplayedEntity
    {
        int Id { get; }
        // TODO: (core) Override ISlugSupported.GetDisplayName() where applicable (instead of passing "name" to ValidateSlugAsync()).
    }
}
