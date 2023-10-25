namespace Smartstore.Core.Content.Media
{
    public partial class MediaHelper
    {
        #region Static

        private readonly static char[] _invalidFileNameChars = Path.GetInvalidFileNameChars().Concat(new[] { '&' }).ToArray();
        private readonly static char[] _invalidFolderNameChars = Path.GetInvalidPathChars().Concat(new[] { '&', '/', '\\' }).ToArray();

        public static string NormalizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }

            if (fileName.IndexOfAny(_invalidFileNameChars) > -1)
            {
                // Don't allocate if not necessary
                return string.Join('-', fileName.Split(_invalidFileNameChars));
            }
            
            return fileName;
        }

        public static string NormalizeFolderName(string folderName)
        {
            if (string.IsNullOrEmpty(folderName))
            {
                return string.Empty;
            }

            if (folderName.IndexOfAny(_invalidFolderNameChars) > -1)
            {
                // Don't allocate if not necessary
                return string.Join('-', folderName.Split(_invalidFolderNameChars));
            }

            return folderName;
        }

        public static bool CheckUniqueFileName(string title, string ext, string destFileName, out string uniqueName)
        {
            return CheckUniqueFileName(title, ext, new[] { destFileName }, out uniqueName);
        }

        public static bool CheckUniqueFileName(string title, string ext, ICollection<string> destFileNames, out string uniqueName)
        {
            uniqueName = null;

            if (destFileNames.Count == 0)
            {
                return false;
            }

            int i = 1;
            while (true)
            {
                var test = string.Concat(title, '-', i, '.', ext.TrimStart('.'));
                if (!destFileNames.Contains(test))
                {
                    // Found our gap
                    uniqueName = test;
                    return true;
                }

                i++;
            }
        }

        #endregion

        private readonly IFolderService _folderService;

        public MediaHelper(IFolderService folderService)
        {
            _folderService = folderService;
        }

        public bool TokenizePath(string path, bool normalizeFileName, out MediaPathData data)
        {
            data = null;

            if (path.IsEmpty())
            {
                return false;
            }

            var dir = Path.GetDirectoryName(path);
            if (dir.HasValue())
            {
                var node = _folderService.GetNodeByPath(dir);
                if (node != null)
                {
                    data = new MediaPathData(node, path[(dir.Length + 1)..], normalizeFileName);
                    return true;
                }
            }

            return false;
        }
    }
}