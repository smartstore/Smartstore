namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Represents a localized entity
    /// </summary>
    public interface ILocalizedEntity : INamedEntity
    {
        int Id { get; }
    }
}
