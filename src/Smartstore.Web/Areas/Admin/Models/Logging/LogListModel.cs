using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Web.Modelling;
using System;
using System.Collections.Generic;

namespace Smartstore.Admin.Models.Logging
{
    [LocalizedDisplay("Admin.System.Log.List.")]
    public class LogListModel : ModelBase
    {        
        [LocalizedDisplay("*CreatedOnFrom")]
        public DateTime? CreatedOnFrom { get; set; }

        [LocalizedDisplay("*CreatedOnTo")]
        public DateTime? CreatedOnTo { get; set; }

        [LocalizedDisplay("*Message")]
        public string Message { get; set; }

        [LocalizedDisplay("*LogLevel")]
        public int? LogLevelId { get; set; }

        [LocalizedDisplay("*Logger")]
        public string Logger { get; set; }

        public List<SelectListItem> AvailableLogLevels { get; set; } = new();
    }
}