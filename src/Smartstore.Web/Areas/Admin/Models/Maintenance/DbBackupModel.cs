using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Admin.Models.Maintenance
{
    [LocalizedDisplay("Admin.System.Maintenance.DbBackup.")]
    public class DbBackupModel : ModelBase
    {
        public DbBackupModel(IFile backup)
        {
            Guard.NotNull(backup, nameof(backup));

            Backup = backup;
        }

        public IFile Backup { get; }
        public string DownloadUrl { get; set; }

        [LocalizedDisplay("Admin.Common.FileName")]
        public string Name => Backup.Name;

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("Admin.Common.FileSize")]
        public long Length => Backup.Length;

        [LocalizedDisplay("Admin.Common.FileSize")]
        public string LengthString
            => Prettifier.HumanizeBytes(Backup.Length);

        [LocalizedDisplay("Admin.System.SystemInfo.AppVersion")]
        public Version Version { get; set; }

        [LocalizedDisplay("*MatchesCurrentVersion")]
        public bool MatchesCurrentVersion { get; set; }
    }
}
