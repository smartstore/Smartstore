using System.Globalization;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Web.Api.Models.OData.Media;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on MediaFolder entity. Returns type FolderNodeInfo which wraps and enriches MediaFolder.
    /// </summary>
    public class MediaFoldersController : WebApiController<FolderNodeInfo>
    {
        private readonly IFolderService _folderService;

        public MediaFoldersController(IFolderService folderService)
        {
            _folderService = folderService;
        }

        [HttpGet, ApiQueryable]
        public IActionResult Get()
        {
            var node = _folderService.GetRootNode();
            if (node == null)
            {
                return NotFound($"Cannot find {nameof(MediaFolder)} root entity.");
            }

            // We have no MediaFolder entity. That's why we cannot apply any ODataQueryOptions.
            var nodes = node.FlattenNodes(false);
            // We already have all nodes, so not necessary to auto-include children here.
            var folders = nodes.Select(x => Convert(x, 0));

            return Ok(folders);
        }

        [HttpGet, ApiQueryable]
        public IActionResult Get(int key)
        {
            var node = _folderService.GetNodeById(key);
            if (node == null)
            {
                return NotFound(key, nameof(MediaFolder));
            }

            var folder = Convert(node);

            return Ok(folder);
        }

        [HttpPost, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Post()
        {
            return ErrorResult(null, "POST MediaFolders is not allowed.", Status403Forbidden);
        }

        [HttpPut, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Put()
        {
            return ErrorResult(null, "PUT MediaFolders is not allowed.", Status403Forbidden);
        }

        [HttpPatch, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Patch()
        {
            return ErrorResult(null, "PATCH MediaFolders is not allowed.", Status403Forbidden);
        }

        [HttpDelete, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Delete()
        {
            // Insufficient endpoint. Parameters required but ODataActionParameters not possible here.
            // Query string parameters less good because not part of the EDM.
            return ErrorResult(null, $"DELETE MediaFolders is not allowed. Use action method \"{nameof(DeleteFolder)}\" instead.", Status403Forbidden);
        }

        #region Actions and functions

        private void DeleteFolder()
        {
            throw new NotImplementedException();
        }

        #endregion

        private static FolderNodeInfo Convert(TreeNode<MediaFolderNode> node, int depth = int.MaxValue)
        {
            if (node == null)
            {
                return null;
            }

            var item = MiniMapper.Map<MediaFolderNode, FolderNodeInfo>(node.Value, CultureInfo.InvariantCulture);
            item.HasChildren = node.HasChildren;

            if (node.HasChildren && node.Depth < depth)
            {
                item.Children = node.Children
                    .Select(x => Convert(x))
                    .Where(x => x != null)
                    .ToList();
            }
            else
            {
                // null crashes.
                item.Children = Array.Empty<FolderNodeInfo>();
            }

            return item;
        }
    }
}
