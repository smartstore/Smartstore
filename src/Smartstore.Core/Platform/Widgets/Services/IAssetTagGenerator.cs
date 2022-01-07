using Microsoft.AspNetCore.Html;

namespace Smartstore.Core.Widgets
{
    public interface IAssetTagGenerator
    {
        IHtmlContent GenerateScript(string url);
        IHtmlContent GenerateStylesheet(string url);
    }

    internal sealed class NullAssetTagGenerator : IAssetTagGenerator
    {
        public IHtmlContent GenerateScript(string url) => null;
        public IHtmlContent GenerateStylesheet(string url) => null;
    }
}
