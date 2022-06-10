namespace Smartstore.Core.Content.Menus
{
    public interface IMenuResolver
    {
        int Order { get; }

        Task<bool> ExistsAsync(string menuName);
        IMenu Resolve(string menuName);
    }
}
