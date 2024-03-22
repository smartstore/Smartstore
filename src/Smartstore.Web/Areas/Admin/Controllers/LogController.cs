using Smartstore.Admin.Models.Logging;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Web.Models.DataGrid;
using LogLevel = Smartstore.Core.Logging.LogLevel;

namespace Smartstore.Admin.Controllers
{
    public class LogController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IDbLogService _dbLogService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly AdminAreaSettings _adminAreaSettings;

        private static readonly Dictionary<LogLevel, string> _logLevelHintMap = new()
        {
            { LogLevel.Fatal, "dark" },
            { LogLevel.Error, "danger" },
            { LogLevel.Warning, "warning" },
            { LogLevel.Information, "primary" },
            { LogLevel.Debug, "secondary" },
            { LogLevel.Verbose, "secondary" }
        };

        public LogController(
            SmartDbContext db,
            IDbLogService dbLogService,
            IDateTimeHelper dateTimeHelper,
            AdminAreaSettings adminAreaSettings)
        {
            _db = db;
            _dbLogService = dbLogService;
            _dateTimeHelper = dateTimeHelper;
            _adminAreaSettings = adminAreaSettings;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.System.Log.Read)]
        public IActionResult List()
        {
            return View();
        }

        [HttpPost]
        [Permission(Permissions.System.Log.Read)]
        public async Task<IActionResult> LogList(GridCommand command, LogListModel model)
        {
            DateTime? createdOnFrom = model.CreatedOnFrom != null
                ? _dateTimeHelper.ConvertToUtcTime(model.CreatedOnFrom.Value, _dateTimeHelper.CurrentTimeZone)
                : null;

            DateTime? createdOnTo = model.CreatedOnTo != null
                ? _dateTimeHelper.ConvertToUtcTime(model.CreatedOnTo.Value, _dateTimeHelper.CurrentTimeZone).AddDays(1)
                : null;

            LogLevel? logLevel = model.LogLevelId > 0 ? (LogLevel?)model.LogLevelId : null;

            var query = _db.Logs.AsNoTracking()
                .ApplyDateFilter(createdOnFrom, createdOnTo)
                .ApplyLoggerFilter(model.Logger)
                .ApplyMessageFilter(model.Message)
                .ApplyLevelFilter(logLevel)
                .OrderByDescending(x => x.CreatedOnUtc)
                .ApplyGridCommand(command, false);

            var logItems = await query.ToPagedList(command).LoadAsync();

            var gridModel = new GridModel<LogModel>
            {
                Rows = logItems.Select(PrepareLogModel),
                Total = logItems.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        [Permission(Permissions.System.Log.Delete)]
        public async Task<IActionResult> LogDelete(GridSelection selection)
        {
            var ids = selection.GetEntityIds().ToList();
            var numDeleted = 0;
            if (ids.Count > 0)
            {
                numDeleted = await _db.Logs
                    .Where(x => ids.Contains(x.Id))
                    .ExecuteDeleteAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        [HttpPost]
        [Permission(Permissions.System.Log.Delete)]
        public async Task<IActionResult> LogClear()
        {
            await _dbLogService.ClearLogsAsync();
            NotifySuccess(T("Admin.System.Log.Cleared"));
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.System.Log.Read)]
        public async Task<IActionResult> View(int id)
        {
            var log = await _db.Logs.FindByIdAsync(id);
            if (log == null)
            {
                return RedirectToAction(nameof(List));
            }

            var model = PrepareLogModel(log);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.System.Log.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var log = await _db.Logs.FindByIdAsync(id);
            if (log == null)
            {
                return RedirectToAction(nameof(List));
            }

            _db.Logs.Remove(log);
            await _db.SaveChangesAsync();

            NotifySuccess(T("Admin.System.Log.Deleted"));
            return RedirectToAction(nameof(List));
        }

        private static string TruncateLoggerName(string loggerName)
        {
            if (loggerName.IndexOf('.') < 0)
            {
                return loggerName;
            }

            var name = string.Empty;
            var tokens = loggerName.Split('.');
            for (int i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                name += i == tokens.Length - 1
                    ? token
                    : string.Concat(token.AsSpan(0, 1), "...");
            }

            return name;
        }

        private LogModel PrepareLogModel(Log log)
        {
            var model = new LogModel
            {
                Id = log.Id,
                LogLevelHint = _logLevelHintMap[log.LogLevel],
                LogLevel = log.LogLevel.GetLocalizedEnum(),
                ShortMessage = log.ShortMessage.NullEmpty() ?? log.FullMessage.Truncate(100, "..."),
                FullMessage = log.FullMessage,
                IpAddress = log.IpAddress,
                CustomerId = log.CustomerId,
                CustomerEmail = log.Customer?.Email,
                PageUrl = log.PageUrl,
                ReferrerUrl = log.ReferrerUrl,
                CreatedOn = _dateTimeHelper.ConvertToUserTime(log.CreatedOnUtc, DateTimeKind.Utc),
                Logger = log.Logger,
                LoggerShort = TruncateLoggerName(log.Logger),
                HttpMethod = log.HttpMethod,
                UserName = log.UserName,
                ViewUrl = Url.Action(nameof(View), "Log", new { id = log.Id })
            };

            return model;
        }
    }
}
