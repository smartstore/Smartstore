using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.OData.Formatter;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Web.Api.Models.Media;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on MediaFolder entity. Returns type FolderNodeInfo which wraps and enriches MediaFolder.
    /// </summary>
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status422UnprocessableEntity)]
    public class MediaFoldersController : WebApiController<FolderNodeInfo>
    {
        private readonly IFolderService _folderService;
        private readonly IMediaService _mediaService;

        public MediaFoldersController(IFolderService folderService, IMediaService mediaService)
        {
            _folderService = folderService;
            _mediaService = mediaService;
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

        /// <summary>
        /// Gets a value indicating whether a folder exists.
        /// </summary>
        /// <param name="path" example="content/my-folder">The path of the folder.</param>
        [HttpPost("MediaFolders/FolderExists")]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(bool), Status200OK)]
        public IActionResult FolderExists([FromODataBody, Required] string path)
        {
            try
            {
                var folderExists = _mediaService.FolderExists(path);

                return Ok(folderExists);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Checks the uniqueness of a folder name.
        /// </summary>
        /// <param name="path" example="content/my-folder">The path of the folder.</param>
        [HttpPost("MediaFolders/CheckUniqueFolderName")]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(CheckUniquenessResult), Status200OK)]
        public IActionResult CheckUniqueFolderName([FromODataBody, Required] string path)
        {
            try
            {
                var success = _folderService.CheckUniqueFolderName(path, out var newPath);

                return Ok(new CheckUniquenessResult
                {
                    Result = success,
                    NewPath = newPath
                });
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Gets the root folder node.
        /// </summary>
        [HttpGet("MediaFolders/GetRootNode"), ApiQueryable]
        [Produces(Json)]
        [ProducesResponseType(typeof(FolderNodeInfo), Status200OK)]
        public IActionResult GetRootNode()
        {
            try
            {
                var root = _folderService.GetRootNode();

                return Ok(Convert(root));
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Gets a folder node by path.
        /// </summary>
        /// <param name="path" example="content/my-folder">The path of the folder.</param>
        [HttpPost("MediaFolders/GetNodeByPath"), ApiQueryable]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(FolderNodeInfo), Status200OK)]
        [ProducesResponseType(Status404NotFound)]
        public IActionResult GetNodeByPath([FromODataBody, Required] string path)
        {
            try
            {
                var node = _folderService.GetNodeByPath(path);
                if (node == null)
                {
                    return NotFound($"Cannot find {nameof(MediaFolder)} entity with path {path.NaIfEmpty()}.");
                }

                return Ok(Convert(node));
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Creates a folder.
        /// </summary>
        /// <param name="path" example="content/my-folder">The path of the folder.</param>
        [HttpPost("MediaFolders/CreateFolder"), ApiQueryable]
        [Permission(Permissions.Media.Update)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(FolderNodeInfo), Status201Created)]
        public async Task<IActionResult> CreateFolder([FromODataBody, Required] string path)
        {
            try
            {
                var result = await _mediaService.CreateFolderAsync(path);

                return Created(Convert(result.Node));
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Moves a folder.
        /// </summary>
        /// <param name="path" example="content/my-folder">The path of the folder.</param>
        /// <param name="destinationPath" example="content/my-renamed-folder">The destination folder path.</param>
        [HttpPost("MediaFolders/MoveFolder"), ApiQueryable]
        [Permission(Permissions.Media.Update)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(FolderNodeInfo), Status200OK)]
        public async Task<IActionResult> MoveFolder(
            [FromODataBody, Required] string path,
            [FromODataBody, Required] string destinationPath)
        {
            try
            {
                var result = await _mediaService.MoveFolderAsync(path, destinationPath);

                return Ok(Convert(result.Node));
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Copies a folder.
        /// </summary>
        /// <param name="path" example="content/my-folder">The path of the folder.</param>
        /// <param name="destinationPath" example="content/my-new-folder">The destination folder path.</param>
        /// <param name="duplicateEntryHandling" example="0">A value indicating how to proceed if the destination folder already exists.</param>
        [HttpPost("MediaFolders/CopyFolder"), ApiQueryable]
        [Permission(Permissions.Media.Update)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(MediaFolderOperationResult), Status200OK)]
        public async Task<IActionResult> CopyFolder(
            [FromODataBody, Required] string path,
            [FromODataBody, Required] string destinationPath,
            [FromODataBody] DuplicateEntryHandling duplicateEntryHandling = DuplicateEntryHandling.Skip)
        {
            try
            {
                var result = await _mediaService.CopyFolderAsync(path, destinationPath, duplicateEntryHandling);

                var opResult = new MediaFolderOperationResult
                {
                    FolderId = result.Folder.Id,
                    Folder = Convert(result.Folder.Node),
                    DuplicateFiles = result.DuplicateFiles
                        .Select(x => new MediaFolderOperationResult.DuplicateFileInfo
                        {
                            SourceFileId = x.SourceFile.Id,
                            DestinationFileId = x.DestinationFile.Id,
                            //SourceFile = Convert(x.SourceFile),
                            //DestinationFile = Convert(x.DestinationFile),
                            UniquePath = x.UniquePath
                        })
                        .ToList()
                };

                return Ok(opResult);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        /// <summary>
        /// Deletes a folder.
        /// </summary>
        /// <param name="path" example="content/my-folder">The path of the folder.</param>
        /// <param name="fileHandling" example="0">A value indicating how to proceed with the files of the deleted folder.</param>
        [HttpPost("MediaFolders/DeleteFolder")]
        [Permission(Permissions.Media.Delete)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(MediaFolderDeleteResult), Status200OK)]
        public async Task<IActionResult> DeleteFolder(
            [FromODataBody, Required] string path,
            [FromODataBody] FileHandling fileHandling = FileHandling.SoftDelete)
        {
            try
            {
                var result = await _mediaService.DeleteFolderAsync(path, fileHandling);

                var opResult = new MediaFolderDeleteResult
                {
                    DeletedFileNames = result.DeletedFileNames,
                    DeletedFolderIds = result.DeletedFolderIds
                };

                return Ok(opResult);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
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
