using Smartstore.Core.Configuration;

namespace Smartstore.Core.Common.Configuration
{
    public class AdminAreaSettings : ISettings
    {
        public int GridPageSize { get; set; } = 25;

        public bool DisplayProductPictures { get; set; } = true;
    }
}
