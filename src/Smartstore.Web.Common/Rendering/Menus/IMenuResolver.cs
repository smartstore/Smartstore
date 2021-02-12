using System.Threading.Tasks;

namespace Smartstore.Web.Rendering
{
    public interface IMenuResolver
    {
        int Order { get; }

        Task<bool> ExistsAsync(string menuName);
        IMenu Resolve(string menuName);
    }
}
