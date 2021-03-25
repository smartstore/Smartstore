using Smartstore.Core.Configuration;

namespace Smartstore.Core.Common.Settings
{
    public class AdminAreaSettings : ISettings
    {
        public int GridPageSize { get; set; } = 25;

        public bool DisplayProductPictures { get; set; } = true;
    }
}
