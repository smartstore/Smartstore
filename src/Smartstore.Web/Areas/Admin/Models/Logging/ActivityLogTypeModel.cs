﻿namespace Smartstore.Admin.Models.Logging
{
    [LocalizedDisplay("Admin.Configuration.ActivityLog.ActivityLogType.Fields.")]
    public class ActivityLogTypeModel : EntityModelBase
    {
        public string SystemKeyword { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Enabled")]
        public bool Enabled { get; set; }
    }
}
