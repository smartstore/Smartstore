namespace Smartstore.Core.Seo
{
    /// <summary>
    /// Represents an entity which supports slug (SEO friendly one-word URLs)
    /// </summary>
    public interface ISlugSupported : IDisplayedEntity
    {
        int Id { get; }
    }
}
