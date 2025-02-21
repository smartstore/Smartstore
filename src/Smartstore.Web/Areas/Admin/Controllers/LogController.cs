using Smartstore.Admin.Models.Logging;
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
            IDateTimeHelper dateTimeHelper)
        {
            _db = db;
            _dbLogService = dbLogService;
            _dateTimeHelper = dateTimeHelper;
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
            var loggerNamesMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var logItems = await _db.Logs
                .AsNoTracking()
                .ApplyDateFilter(createdOnFrom, createdOnTo)
                .ApplyLoggerFilter(model.Logger)
                .ApplyMessageFilter(model.Message)
                .ApplyLevelFilter(logLevel)
                .OrderByDescending(x => x.CreatedOnUtc)
                .SelectSummary()
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = logItems
                .Select(x => CreateLogModel(x, true, loggerNamesMap))
                .ToList();

            var gridModel = new GridModel<LogModel>
            {
                Rows = rows,
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

            var model = CreateLogModel(log, false);

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

        private static string GetShortLoggerName(string name)
        {
            if (name == null || name.IndexOf('.') < 0)
            {
                return name;
            }

            var result = string.Empty;
            var tokens = name.Split('.');
            for (int i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                result += i == tokens.Length - 1
                    ? token
                    : string.Concat(token.AsSpan(0, 1), "…");
            }

            return result;
        }

        private LogModel CreateLogModel(Log log, bool forList, Dictionary<string, string> loggerNamesMap = null)
        {
            var model = new LogModel
            {
                Id = log.Id,
                LogLevelHint = _logLevelHintMap[log.LogLevel],
                LogLevel = log.LogLevel.GetLocalizedEnum(),
                ShortMessage = log.ShortMessage.NullEmpty() ?? log.FullMessage.Truncate(100, "…"),
                IpAddress = log.IpAddress,
                CustomerId = log.CustomerId,
                CustomerEmail = log.Customer?.Email,
                CreatedOn = _dateTimeHelper.ConvertToUserTime(log.CreatedOnUtc, DateTimeKind.Utc),
                Logger = log.Logger,
                HttpMethod = log.HttpMethod,
                UserName = log.UserName,
                UserAgent = log.UserAgent,
                ViewUrl = Url.Action(nameof(View), "Log", new { id = log.Id })
            };

            if (loggerNamesMap != null)
            {
                if (loggerNamesMap.TryGetValue(log.Logger, out var loggerName))
                {
                    model.ShortLoggerName = loggerName;
                }
                else
                {
                    model.ShortLoggerName = loggerNamesMap[log.Logger] = GetShortLoggerName(log.Logger);
                }
            }
            else
            {
                model.ShortLoggerName = GetShortLoggerName(log.Logger);
            }

            if (!forList)
            {
                model.FullMessage = log.FullMessage;
                model.PageUrl = log.PageUrl;
                model.ReferrerUrl = log.ReferrerUrl;
            }

            return model;
        }
    }
}
