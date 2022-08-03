namespace Smartstore.Admin.Models.Common
{
    public enum LibraryLicense
    {
        Apache2,
        BSD3,
        CreativeCommons,
        CreativeCommons4,
        Freedom,
        GPLv3,
        LGPL,
        MIT,
        MPL20,
        MSPL,
        zlib
    }

    public class LibraryModel
    {
        public LibraryModel()
        {
        }

        public LibraryModel(string name, string url, LibraryLicense license, string copyright)
        {
            Name = name;
            Url = url;
            License = license;
            Copyright = copyright;
        }

        public string Name { get; set; }
        public string Url { get; set; }
        public LibraryLicense License { get; set; }
        public string Copyright { get; set; }
    }
}
