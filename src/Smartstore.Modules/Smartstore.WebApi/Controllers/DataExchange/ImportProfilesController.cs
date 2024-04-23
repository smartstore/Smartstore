using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using Smartstore.Core;
using Smartstore.Core.DataExchange;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Core.Stores;
using Smartstore.IO;
using Smartstore.Scheduling;
using Smartstore.Threading;

namespace Smartstore.Web.Api.Controllers
{
    /// <summary>
    /// The endpoint for operations on ImportProfile entity.
    /// </summary>
    [WebApiGroup(WebApiGroupNames.Platform)]
    public class ImportProfilesController : WebApiController<ImportProfile>
    {
        private readonly Lazy<IWorkContext> _workContext;
        private readonly Lazy<IStoreContext> _storeContext;
        private readonly Lazy<IImportProfileService> _importProfileService;
        private readonly Lazy<IPermissionService> _permissionService;
        private readonly Lazy<ITaskScheduler> _taskScheduler;

        public ImportProfilesController(
            Lazy<IWorkContext> workContext,
            Lazy<IStoreContext> storeContext,
            Lazy<IImportProfileService> importProfileService,
            Lazy<IPermissionService> permissionService,
            Lazy<ITaskScheduler> taskScheduler)
        {
            _workContext = workContext;
            _storeContext = storeContext;
            _importProfileService = importProfileService;
            _permissionService = permissionService;
            _taskScheduler = taskScheduler;
        }

        [HttpGet("ImportProfiles"), ApiQueryable]
        [Permission(Permissions.Configuration.Import.Read)]
        public IQueryable<ImportProfile> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet("ImportProfiles({key})"), ApiQueryable]
        [Permission(Permissions.Configuration.Import.Read)]
        public SingleResult<ImportProfile> Get(int key)
        {
            return GetById(key);
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
            return Forbidden();
        }

        #region Actions and functions

        /// <summary>
        /// Saves import files and assigns them to an ImportProfile.
        /// </summary>
        /// <param name="key">
        /// Identifier of the import profile to which the files should be assigned.
        /// 0 if the profile is to be identified by name.
        /// </param>
        /// <param name="files">The files to be saved.</param>
        /// <param name="name">Optional name by which the import profile is to be identified.</param>
        /// <param name="clearDirectory">A value indicating whether to clear the content directory of the profile.</param>
        /// <param name="startImport">A value indicating whether the import should be started.</param>
        [HttpPost("ImportProfiles({key})/SaveFiles"), ApiQueryable]
        [Permission(Permissions.Configuration.Import.Execute)]
        [Consumes("multipart/form-data"), Produces(Json)]
        [ProducesResponseType(typeof(ImportProfile), Status200OK)]
        [ProducesResponseType(Status415UnsupportedMediaType)]
        [ProducesResponseType(Status422UnprocessableEntity)]
        public async Task<IActionResult> SaveFiles(int key,
            [Required] IFormFileCollection files,
            [FromQuery] string name = null,
            [FromForm] bool clearDirectory = false,
            [FromForm] bool startImport = false)
        {
            if (!HasMultipartContent)
            {
                return StatusCode(Status415UnsupportedMediaType);
            }

            try
            {
                files = Request.Form.Files;
                if (files.Count == 0)
                {
                    return BadRequest("Missing multipart file data.");
                }
                if (files.Any(x => x.FileName.IsEmpty()))
                {
                    return BadRequest("The file name is missing.");
                }

                var entity = (ImportProfile)null;

                if (key != 0)
                {
                    entity = await GetRequiredById(key);
                }
                else if (name.HasValue())
                {
                    entity = await Entities.FirstOrDefaultAsync(x => x.Name == name);
                }

                if (entity == null)
                {
                    return NotFound($"Cannot find {nameof(ImportProfile)} entity. Please specify a valid ID or profile name.");
                }

                var targetDir = await _importProfileService.Value.GetImportDirectoryAsync(entity, "Content", true);

                if (clearDirectory)
                {
                    targetDir.FileSystem.ClearDirectory(targetDir, false, TimeSpan.Zero);
                }

                var lockKey = $"apiimportprofile:{entity.Id}";
                if (AsyncLock.IsLockHeld(lockKey))
                {
                    return ErrorResult(null, $"API cannot write to {targetDir.SubPath} of import profile with ID {entity.Id} ({entity.Name}). The directory is locked.");
                }

                using (await AsyncLock.KeyedAsync(lockKey, TimeSpan.FromSeconds(60)))
                {
                    await CopyFiles(files, targetDir);
                }

                if (startImport)
                {
                    var customer = _workContext.Value.CurrentCustomer;

                    if (await _permissionService.Value.AuthorizeAsync(Permissions.System.ScheduleTask.Execute, customer))
                    {
                        _ = _taskScheduler.Value.RunSingleTaskAsync(entity.TaskId, new Dictionary<string, string>
                        {
                            { TaskExecutor.CurrentCustomerIdParamName, customer.Id.ToString() },
                            { TaskExecutor.CurrentStoreIdParamName, _storeContext.Value.CurrentStore.Id.ToString() }
                        });
                    }
                }

                return Ok(entity);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex);
            }
        }

        private static async Task CopyFiles(IFormFileCollection files, IDirectory targetDir)
        {
            foreach (var file in files)
            {
                var fileName = PathUtility.SanitizeFileName(file.FileName);
                var extension = Path.GetExtension(fileName);

                if (extension.EqualsNoCase(".zip"))
                {
                    using var stream = file.OpenReadStream();
                    using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

                    foreach (var entry in archive.Entries)
                    {
                        if (Path.GetFileName(entry.FullName).Length == 0)
                        {
                            // Entry is a directory.
                            if (entry.Length == 0)
                            {
                                await targetDir.CreateSubdirectoryAsync(entry.FullName);
                            }
                        }
                        else
                        {
                            // Entry is a file.
                            var targetFile = await targetDir.GetFileAsync(entry.FullName);
                            using var targetStream = await targetFile.OpenWriteAsync();

                            using var sourceStream = entry.Open();
                            await sourceStream.CopyToAsync(targetStream);
                        }
                    }
                }
                else
                {
                    var targetFile = await targetDir.GetFileAsync(file.FileName);
                    using var targetStream = await targetFile.OpenWriteAsync();

                    using var sourceStream = file.OpenReadStream();
                    await sourceStream.CopyToAsync(targetStream);
                }
            }
        }

        #endregion
    }
}
