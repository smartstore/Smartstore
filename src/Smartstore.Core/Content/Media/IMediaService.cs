using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Content.Media.Storage;

namespace Smartstore.Core.Content.Media
{
    #region Enums

    [Flags]
    public enum MediaLoadFlags
    {
        None = 0,
        WithBlob = 1 << 0,
        WithTags = 1 << 1,
        WithTracks = 1 << 2,
        WithFolder = 1 << 3,
        AsNoTracking = 1 << 4,
        Full = WithBlob | WithTags | WithTracks | WithFolder,
        FullNoTracking = Full | AsNoTracking
    }

    public enum SpecialMediaFolder
    {
        AllFiles = -500,
        Trash = -400,
        Orphans = -300,
        TransientFiles = -200,
        UnassignedFiles = -100
    }

    public enum FileHandling
    {
        SoftDelete,
        MoveToRoot,
        Delete
    }

    public enum DuplicateFileHandling
    {
        ThrowError,
        Overwrite,
        Rename
    }

    public enum DuplicateEntryHandling
    {
        ThrowError,
        Overwrite,
        // Folder: Overwrite, File: Rename
        Rename,
        Skip
    }

    public enum MimeValidationType
    {
        NoValidation,
        MimeTypeMustMatch,
        MediaTypeMustMatch
    }

    #endregion

    #region Result & Cargo objects

    public class DuplicateFileInfo
    {
        [JsonProperty("source")]
        public MediaFileInfo SourceFile { get; set; }

        [JsonProperty("dest")]
        public MediaFileInfo DestinationFile { get; set; }

        [JsonProperty("uniquePath")]
        public string UniquePath { get; set; }
    }

    public class FolderOperationResult
    {
        public string Operation { get; set; }
        public MediaFolderInfo Folder { get; set; }
        public DuplicateEntryHandling DuplicateEntryHandling { get; set; }
        public IList<DuplicateFileInfo> DuplicateFiles { get; set; }
    }

    public class FolderDeleteResult
    {
        public HashSet<int> DeletedFolderIds { get; set; } = new HashSet<int>();
        public IList<string> DeletedFileNames { get; set; } = new List<string>();
        public IList<string> TrackedFileNames { get; set; } = new List<string>();
        public IList<string> LockedFileNames { get; set; } = new List<string>();
    }

    public class FileOperationResult
    {
        public string Operation { get; set; }
        public MediaFileInfo SourceFile { get; set; }
        public MediaFileInfo DestinationFile { get; set; }
        public DuplicateFileHandling DuplicateFileHandling { get; set; }
        public bool IsDuplicate { get; set; }
        public string UniquePath { get; set; }
    }

    #endregion
}
