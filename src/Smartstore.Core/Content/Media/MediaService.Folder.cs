using System.Linq.Dynamic.Core;
using System.Runtime.CompilerServices;
using Smartstore.Collections;
using Smartstore.Data;

namespace Smartstore.Core.Content.Media
{
    public partial class MediaService : IMediaService
    {
        #region Folder

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FolderExists(string path)
        {
            return _folderService.GetNodeByPath(path) != null;
        }

        public async Task<MediaFolderInfo> CreateFolderAsync(string path)
        {
            Guard.NotEmpty(path);

            path = FolderService.NormalizePath(path, false);
            ValidateFolderPath(path, "CreateFolder", nameof(path));

            var dupe = _folderService.GetNodeByPath(path);
            if (dupe != null)
            {
                throw _exceptionFactory.DuplicateFolder(path, dupe.Value);
            }

            var sep = "/";
            var folderNames = path.Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries);
            bool flag = false;
            int folderId = 0;

            path = string.Empty;

            for (int i = 0; i < folderNames.Length; i++)
            {
                var folderName = MediaHelper.NormalizeFolderName(folderNames[i]);
                path += (i > 0 ? sep : string.Empty) + folderName;

                if (!flag)
                {
                    // Find the last existing node in path trail
                    var currentNode = _folderService.GetNodeByPath(path)?.Value;
                    if (currentNode != null)
                    {
                        folderId = currentNode.Id;
                    }
                    else
                    {
                        if (i == 0) throw new NotSupportedException(T("Admin.Media.Exception.TopLevelAlbum", path));
                        flag = true;
                    }
                }

                if (flag)
                {
                    using (new DbContextScope(_db, deferCommit: false))
                    {
                        var mediaFolder = new MediaFolder { Name = folderName, ParentId = folderId };
                        _db.MediaFolders.Add(mediaFolder);
                        await _db.SaveChangesAsync();
                        folderId = mediaFolder.Id;
                    }
                }
            }

            return ConvertMediaFolder(_folderService.GetNodeById(folderId));
        }

        public async Task<MediaFolderInfo> MoveFolderAsync(string path, string destinationPath)
        {
            Guard.NotEmpty(path, nameof(path));
            Guard.NotEmpty(destinationPath, nameof(destinationPath));

            path = FolderService.NormalizePath(path);
            ValidateFolderPath(path, "MoveFolder", nameof(path));

            var node = _folderService.GetNodeByPath(path);
            if (node == null)
            {
                throw _exceptionFactory.FolderNotFound(path);
            }

            if (node.Value.IsAlbum)
            {
                throw new NotSupportedException(T("Admin.Media.Exception.AlterRootAlbum", node.Value.Name));
            }

            var folder = await _db.MediaFolders.FindByIdAsync(node.Value.Id);
            if (folder == null)
            {
                throw _exceptionFactory.FolderNotFound(path);
            }

            ValidateFolderPath(destinationPath, "MoveFolder", nameof(destinationPath));

            // Destination must not exist
            if (FolderExists(destinationPath))
            {
                throw new ArgumentException(T("Admin.Media.Exception.DuplicateFolder", destinationPath));
            }

            var destParent = FolderService.NormalizePath(Path.GetDirectoryName(destinationPath));

            // Get destination parent
            var destParentNode = _folderService.GetNodeByPath(destParent);
            if (destParentNode == null)
            {
                throw _exceptionFactory.FolderNotFound(destinationPath);
            }

            // Cannot move outside source album
            if (!_folderService.AreInSameAlbum(folder.Id, destParentNode.Value.Id))
            {
                throw _exceptionFactory.NotSameAlbum(node.Value.Path, destParent);
            }

            if (destParentNode.IsDescendantOfOrSelf(node))
            {
                throw new ArgumentException(T("Admin.Media.Exception.DescendantFolder", destinationPath, node.Value.Path));
            }

            // Set new values
            folder.ParentId = destParentNode.Value.Id;
            folder.Name = Path.GetFileName(destinationPath);

            // Commit
            await _db.SaveChangesAsync();

            return ConvertMediaFolder(_folderService.GetNodeById(folder.Id));
        }

        public async Task<FolderOperationResult> CopyFolderAsync(
            string path,
            string destinationPath,
            DuplicateEntryHandling dupeEntryHandling = DuplicateEntryHandling.Skip,
            CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(path, nameof(path));
            Guard.NotEmpty(destinationPath, nameof(destinationPath));

            path = FolderService.NormalizePath(path);
            ValidateFolderPath(path, "CopyFolder", nameof(path));

            destinationPath = FolderService.NormalizePath(destinationPath);
            if (destinationPath.EnsureEndsWith('/').StartsWith(path.EnsureEndsWith('/')))
            {
                throw new ArgumentException(T("Admin.Media.Exception.DescendantFolder", destinationPath, path), nameof(destinationPath));
            }

            var node = _folderService.GetNodeByPath(path);
            if (node == null)
            {
                throw _exceptionFactory.FolderNotFound(path);
            }

            if (node.Value.IsAlbum)
            {
                throw new NotSupportedException(T("Admin.Media.Exception.CopyRootAlbum", node.Value.Name));
            }

            using (var scope = new DbContextScope(_db, autoDetectChanges: false, deferCommit: true))
            {
                destinationPath += "/" + node.Value.Name;
                var dupeFiles = new List<DuplicateFileInfo>();

                // >>>> Do the heavy stuff
                var folder = await InternalCopyFolder(scope, node, destinationPath, dupeEntryHandling, dupeFiles, cancelToken);

                var result = new FolderOperationResult
                {
                    Operation = "copy",
                    DuplicateEntryHandling = dupeEntryHandling,
                    Folder = folder,
                    DuplicateFiles = dupeFiles
                };

                return result;
            }
        }

        private async Task<MediaFolderInfo> InternalCopyFolder(
            DbContextScope scope,
            TreeNode<MediaFolderNode> sourceNode,
            string destPath,
            DuplicateEntryHandling dupeEntryHandling,
            IList<DuplicateFileInfo> dupeFiles,
            CancellationToken cancelToken = default)
        {
            // Get dest node
            var destNode = _folderService.GetNodeByPath(destPath);

            // Dupe handling
            if (destNode != null && dupeEntryHandling == DuplicateEntryHandling.ThrowError)
            {
                throw _exceptionFactory.DuplicateFolder(sourceNode.Value.Path, destNode.Value);
            }

            // Use IMediaDupeDetector to get all files in destination folder for faster dupe selection.
            using var dupeDetector = destNode != null ? _dupeDetectorFactory.GetDetector(destNode.Value.Id) : null;

            // Create dest folder
            destNode ??= await CreateFolderAsync(destPath);

            // INFO: we gonna change file name during the files loop later.
            var destPathData = new MediaPathData(destNode, "placeholder.txt");

            // Get all source files in one go
            var files = await _searcher.SearchFiles(
                new MediaSearchQuery { FolderId = sourceNode.Value.Id },
                MediaLoadFlags.AsNoTracking | MediaLoadFlags.WithTags).LoadAsync(false, cancelToken);

            // Holds source and copy together, 'cause we perform a two-pass copy (file first, then data)
            var tuples = new List<(MediaFile, MediaFile)>(500);

            // Copy files batched
            foreach (var batch in files.Chunk(500))
            {
                if (cancelToken.IsCancellationRequested)
                    break;

                foreach (var file in batch)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    destPathData.FileName = file.Name;

                    // >>> Do copy
                    var copyResult = await InternalCopyFile(
                        file,
                        destPathData,
                        false /* copyData */,
                        dupeEntryHandling,
                        () => dupeDetector?.DetectFileAsync(file.Name, cancelToken),
                        pd => dupeDetector?.CheckUniqueFileNameAsync(pd, cancelToken));

                    if (copyResult.Copy != null)
                    {
                        if (copyResult.IsDupe)
                        {
                            dupeFiles.Add(new DuplicateFileInfo
                            {
                                SourceFile = ConvertMediaFile(file, sourceNode.Value),
                                DestinationFile = ConvertMediaFile(copyResult.Copy, destNode.Value),
                                UniquePath = destPathData.FullPath
                            });
                        }
                        if (!copyResult.IsDupe || dupeEntryHandling != DuplicateEntryHandling.Skip)
                        {
                            // When dupe: add to processing queue only if file was NOT skipped
                            tuples.Add((file, copyResult.Copy));
                        }
                    }
                }

                if (!cancelToken.IsCancellationRequested)
                {
                    // Save batch to DB (1st pass)
                    await scope.CommitAsync(cancelToken);

                    // Now copy file data
                    foreach (var op in tuples)
                    {
                        await InternalCopyFileData(op.Item1, op.Item2);
                    }

                    // Save batch to DB (2nd pass)
                    await scope.CommitAsync(cancelToken);
                }

                _db.DetachEntities<MediaFolder>();
                _db.DetachEntities<MediaFile>();
                tuples.Clear();
            }

            // Copy folders
            foreach (var node in sourceNode.Children)
            {
                if (cancelToken.IsCancellationRequested)
                    break;

                destPath = destNode.Value.Path + "/" + node.Value.Name;
                await InternalCopyFolder(scope, node, destPath, dupeEntryHandling, dupeFiles, cancelToken);
            }

            return ConvertMediaFolder(destNode);
        }

        public async Task<FolderDeleteResult> DeleteFolderAsync(
            string path,
            FileHandling fileHandling = FileHandling.SoftDelete,
            CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(path, nameof(path));

            path = FolderService.NormalizePath(path);
            ValidateFolderPath(path, "DeleteFolder", nameof(path));

            var root = _folderService.GetNodeByPath(path);
            if (root == null)
            {
                throw _exceptionFactory.FolderNotFound(path);
            }

            // Collect all affected subfolders also
            var allNodes = root.FlattenNodes(true).Reverse().ToArray();
            var result = new FolderDeleteResult();

            using (var scope = new DbContextScope(_db, autoDetectChanges: false, deferCommit: true))
            {
                // Delete all from DB
                foreach (var node in allNodes)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    var folder = await _db.MediaFolders.FindByIdAsync(node.Value.Id);
                    if (folder != null)
                    {
                        await InternalDeleteFolder(scope, folder, node, root, result, fileHandling, cancelToken);
                    }
                }
            }

            return result;
        }

        private async Task InternalDeleteFolder(
            DbContextScope scope,
            MediaFolder folder,
            TreeNode<MediaFolderNode> node,
            TreeNode<MediaFolderNode> root,
            FolderDeleteResult result,
            FileHandling strategy,
            CancellationToken cancelToken = default)
        {
            // (perf) We gonna check file tracks, so we should preload all tracks.
            await _db.LoadCollectionAsync(folder, (MediaFolder x) => x.Files, false, q => q.Include(f => f.Tracks));

            var files = folder.Files.ToList();
            var lockedFiles = new List<MediaFile>(files.Count);
            var trackedFiles = new List<MediaFile>(files.Count);

            // First delete files
            if (folder.Files.Any())
            {
                var albumId = strategy == FileHandling.MoveToRoot
                    ? _folderService.FindAlbum(folder.Id).Value.Id
                    : (int?)null;

                foreach (var batch in files.Chunk(500))
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    foreach (var file in batch)
                    {
                        if (cancelToken.IsCancellationRequested)
                            break;

                        if (strategy == FileHandling.Delete && file.Tracks.Any())
                        {
                            // Don't delete tracked files
                            trackedFiles.Add(file);
                            continue;
                        }

                        if (strategy == FileHandling.Delete)
                        {
                            try
                            {
                                result.DeletedFileNames.Add(file.Name);
                                await DeleteFileAsync(file, true);
                            }
                            catch (DeleteTrackedFileException)
                            {
                                trackedFiles.Add(file);
                            }
                            catch (IOException)
                            {
                                lockedFiles.Add(file);
                            }
                        }
                        else if (strategy == FileHandling.SoftDelete)
                        {
                            await DeleteFileAsync(file, false);
                            file.FolderId = null;
                            result.DeletedFileNames.Add(file.Name);
                        }
                        else if (strategy == FileHandling.MoveToRoot)
                        {
                            file.FolderId = albumId;
                            result.DeletedFileNames.Add(file.Name);
                        }
                    }

                    await scope.CommitAsync(cancelToken);
                }

                if (lockedFiles.Any())
                {
                    // Retry deletion of failed files due to locking.
                    // INFO: By default "LocalFileSystem" waits for 500ms until the lock is revoked or it throws.
                    foreach (var lockedFile in lockedFiles.ToArray())
                    {
                        if (cancelToken.IsCancellationRequested)
                            break;

                        try
                        {
                            await DeleteFileAsync(lockedFile, true);
                            lockedFiles.Remove(lockedFile);
                        }
                        catch { }
                    }

                    await scope.CommitAsync(cancelToken);
                }
            }

            if (!cancelToken.IsCancellationRequested && lockedFiles.Count > 0)
            {
                var fullPath = CombinePaths(root.Value.Path, lockedFiles[0].Name);
                throw new IOException(T("Admin.Media.Exception.InUse", fullPath));
            }

            if (!cancelToken.IsCancellationRequested && lockedFiles.Count == 0 && trackedFiles.Count == 0 && node.Children.All(x => result.DeletedFolderIds.Contains(x.Value.Id)))
            {
                // Don't delete folder if a containing file could not be deleted, 
                // any tracked file was found or any of its child folders could not be deleted..
                _db.MediaFolders.Remove(folder);
                await scope.CommitAsync(cancelToken);
                result.DeletedFolderIds.Add(folder.Id);
            }

            result.LockedFileNames = lockedFiles.Select(x => x.Name).ToList();
            result.TrackedFileNames = trackedFiles.Select(x => x.Name).ToList();
        }

        #endregion

        #region Utils

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MediaFolderInfo ConvertMediaFolder(TreeNode<MediaFolderNode> node)
        {
            return new MediaFolderInfo(node, this, _searcher, _folderService, _exceptionFactory);
        }

        private void ValidateFolderPath(string path, string operation, string paramName)
        {
            if (!IsPath(path))
            {
                // Destination cannot be an album                
                throw new ArgumentException(T("Admin.Media.Exception.PathSpecification", path, operation), paramName);
            }
        }

        #endregion
    }
}
