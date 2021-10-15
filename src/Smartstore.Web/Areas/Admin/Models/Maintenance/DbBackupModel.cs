using System;
using Smartstore.IO;
using Smartstore.Utilities;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Maintenance
{
    [LocalizedDisplay("Admin.System.Maintenance.Backup")]
    public class DbBackupModel : ModelBase
    {
        public DbBackupModel(IFile backup)
        {
            Guard.NotNull(backup, nameof(backup));

            Backup = backup;
        }

        public IFile Backup { get; }

        [LocalizedDisplay("Common.UpdatedOn")]
        public DateTime UpdatedOn { get; set; }

        [LocalizedDisplay("*Length")]
        public string LengthString
            => Prettifier.HumanizeBytes(Backup.Length);
    }
}
