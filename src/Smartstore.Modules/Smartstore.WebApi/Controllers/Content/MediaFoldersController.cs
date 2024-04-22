using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.OData.Formatter;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core.Content.Media;
using Smartstore.Web.Api.Models.Media;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on MediaFolder entity. Returns type FolderNodeInfo which wraps and enriches MediaFolder.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Content)]
    [ProducesResponseType(Status422UnprocessableEntity)]
    public class MediaFoldersController : WebApiController<MediaFolder>
    {
        private readonly IFolderService _folderService;
        private readonly IMediaService _mediaService;

        public MediaFoldersController(IFolderService folderService, IMediaService mediaService)
        {
            _folderService = folderService;
            _mediaService = mediaService;
        }

        [HttpGet("MediaFolders")]
        [ProducesResponseType(typeof(IEnumerable<FolderNodeInfo>), Status200OK)]
        public IActionResult Get()
        {
            try
            {
                var node = _folderService.GetRootNode();
                if (node == null)
                {
                    return NotFound($"Cannot find {nameof(MediaFolder)} root entity.");
                }

                // We cannot apply any ODataQueryOptions because we have no MediaFolder entity anymore.
                var nodes = node.FlattenNodes(false);
                var folders = nodes.Select(Convert);

                return Ok(folders);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        [HttpGet("MediaFolders({key})"), ApiQueryable]
        public IActionResult Get(int key)
        {
            try
            {
                var node = _folderService.GetNodeById(key);
                if (node == null)
                {
                    return NotFound(key);
                }

                return Ok(Convert(node));
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        [HttpPost, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Post()
        {
            return Forbidden();
        }

        [HttpPut, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Put()
        {
            return Forbidden();
        }

        [HttpPatch, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Patch()
        {
            return Forbidden();
        }

        [HttpDelete, ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Delete()
        {
            // Insufficient endpoint. Parameters required but ODataActionParameters not possible here.
            // Query string parameters less good because not part of the EDM.
            return Forbidden($"Use endpoint \"{nameof(DeleteFolder)}\" instead.");
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
        [HttpGet("MediaFolders/GetRootNode")]
        [Produces(Json)]
        [ProducesResponseType(typeof(FolderNodeInfo), Status200OK)]
        public IActionResult GetRootNode()
        {
            try
            {
                var node = _folderService.GetRootNode();
                if (node == null)
                {
                    return NotFound($"Cannot find {nameof(MediaFolder)} root entity.");
                }

                return Ok(Convert(node));
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
        [HttpPost("MediaFolders/GetNodeByPath")]
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
        [HttpPost("MediaFolders/CreateFolder")]
        [Permission(Permissions.Media.Update)]
        [Consumes(Json), Produces(Json)]
        [ProducesResponseType(typeof(FolderNodeInfo), Status201Created)]
        public async Task<IActionResult> CreateFolder([FromODataBody, Required] string path)
        {
            try
            {
                var result = await _mediaService.CreateFolderAsync(path);
                var url = BuildUrl(result.Id);

                return Created(url, Convert(result.Node));
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
        [HttpPost("MediaFolders/MoveFolder")]
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
        [HttpPost("MediaFolders/CopyFolder")]
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

        private static FolderNodeInfo Convert(TreeNode<MediaFolderNode> node)
        {
            if (node == null)
            {
                return null;
            }

            var item = MiniMapper.Map<MediaFolderNode, FolderNodeInfo>(node.Value, CultureInfo.InvariantCulture);
            item.HasChildren = node.HasChildren;

            return item;
        }
    }
}
