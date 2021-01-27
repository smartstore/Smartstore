namespace Smartstore.Web.Modelling
{
    /// <summary>
    /// Interface for custom model attributes that are managed by custom metadata providers.
    /// </summary>
    public interface IModelAttribute
    {
        string Name { get; }
    }
}
