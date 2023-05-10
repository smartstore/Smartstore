using Smartstore.Collections;
using Smartstore.IO;

namespace Smartstore.Core.Content.Media
{
    public class MediaPathData
    {
        private string _name;
        private string _title;
        private string _ext;
        private string _mime;

        public MediaPathData(TreeNode<MediaFolderNode> node, string fileName, bool normalizeFileName = false)
        {
            Guard.NotNull(node);
            Guard.NotEmpty(fileName);

            Node = node;
            _name = normalizeFileName
                ? Uri.UnescapeDataString(MediaHelper.NormalizeFileName(fileName))
                : Uri.UnescapeDataString(fileName);
        }

        public MediaPathData(string path, bool normalizeFileName = false)
        {
            Guard.NotEmpty(path);

            _name = normalizeFileName
                ? Uri.UnescapeDataString(MediaHelper.NormalizeFileName(Path.GetFileName(path)))
                : Uri.UnescapeDataString(Path.GetFileName(path));
        }

        public MediaPathData(MediaPathData pathData)
        {
            Node = pathData.Node;
            _name = pathData.FileName;
            _title = pathData._title;
            _ext = pathData._ext;
            _mime = pathData._mime;
        }

        public TreeNode<MediaFolderNode> Node { get; }
        public MediaFolderNode Folder => Node.Value;

        public string FileName
        {
            get => _name;
            set
            {
                Guard.NotEmpty(value);

                _name = value;
                _title = null;
                _ext = null;
                _mime = null;
            }
        }

        public string FullPath => Folder.Path + '/' + _name;

        public string FileTitle => _title ??= Path.GetFileNameWithoutExtension(_name);

        public string Extension
        {
            get => _ext ??= Path.GetExtension(_name).EmptyNull().TrimStart('.');
            set => _ext = value?.TrimStart('.');
        }

        public string MimeType
        {
            get => _mime ??= MimeTypes.MapNameToMimeType(_name);
            set => _mime = value;
        }
    }
}