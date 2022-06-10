using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Engine.Modularity;

namespace Smartstore.Licensing
{
    // TODO: (mg) (core) Remove everything in here after Licensing has been ported.

    public enum LicensingState
    {
        Unlicensed = 0,
        Demo = 10,
        Licensed = 20
    }

    public enum UnlicensedActionResult
    {
        Block,
        NotFound,
        Empty
    }

    public static class LicenseChecker
    {
        private static readonly ConcurrentDictionary<Type, LicensableModuleInfo> _cachedModuleInfos = new();

        public static Task<LicenseCheckerResult> ActivateAsync(string licenseKey, string systemName, string url)
        {
            return Task.FromResult(new LicenseCheckerResult 
            {
                State = LicensingState.Licensed,
                Success = true,
                TruncatedLicenseKey = licenseKey
            });
        }

        public static LicenseCheckerResult Check(string systemName, string url = null)
            => new() { State = LicensingState.Licensed, Success = true, TruncatedLicenseKey = "SM-01234-56789" };

        public static Task<LicenseCheckerResult> CheckAsync(string systemName, string url = null)
            => Task.FromResult(new LicenseCheckerResult { State = LicensingState.Licensed, Success = true, TruncatedLicenseKey = "SM-01234-56789" });

        public static LicensingState CheckState(string systemName, string url = null)
            => LicensingState.Licensed;

        public static Task<LicensingState> CheckStateAsync(string systemName, string url = null)
            => Task.FromResult(LicensingState.Licensed);

        public static LicensingData GetLicense(string systemName, string url = null)
            => new() { State = LicensingState.Licensed, TruncatedLicenseKey = "SM-01234-56789" };

        public static Task<LicensingData> GetLicenseAsync(string systemName, string url = null)
            => Task.FromResult(new LicensingData { State = LicensingState.Licensed, TruncatedLicenseKey = "SM-01234-56789" });

        public static bool IsLicensableModule(IModuleDescriptor descriptor)
            => IsLicensableModule(descriptor, out _);

        public static bool IsLicensableModule(IModuleDescriptor descriptor, out bool hasSingleLicenseForAllStores)
        {
            Guard.NotNull(descriptor, nameof(descriptor));

            var info = GetModuleInfo(descriptor);
            if (info != null)
            {
                hasSingleLicenseForAllStores = info.HasSingleLicenseForAllStores;
                return info.IsLicensable;
            }

            hasSingleLicenseForAllStores = false;
            return false;
        }

        private static LicensableModuleInfo GetModuleInfo(IModuleDescriptor descriptor)
        {
            var moduleType = descriptor?.Module?.ModuleType;

            if (moduleType == null)
            {
                return null;
            }

            var info = _cachedModuleInfos.GetOrAdd(moduleType, t =>
            {
                var attr = moduleType.GetAttribute<LicensableModuleAttribute>(false);
                var result = new LicensableModuleInfo
                {
                    ModuleType = moduleType,
                    SystemName = descriptor.SystemName
                };

                if (attr != null)
                {
                    result.IsLicensable = true;
                    result.HasSingleLicenseForAllStores = attr.HasSingleLicenseForAllStores;
                }

                return result;
            });

            return info;
        }

        public static LicenseCheckerResult ResetState(string systemName, string url = null)
            => new() { State = LicensingState.Licensed, Success = true, TruncatedLicenseKey = "SM-01234-56789" };

        public static Task<LicenseCheckerResult> ResetStateAsync(string systemName, string url = null)
            => Task.FromResult(new LicenseCheckerResult { State = LicensingState.Licensed, Success = true, TruncatedLicenseKey = "SM-01234-56789" });
    }

    public class LicenseCheckerResult
    {
        public bool Success { get; set; }
        public string FailureCode { get; set; }
        public string Message { get; set; }
        public string MessageFull { get; set; }
        public bool IsFailureWarning { get; set; }
        public LicensingState State { get; set; }
        public string TruncatedLicenseKey { get; set; }
        public int RemainingDemoDays { get; set; }
        public string StateString { get; }
    }

    public class LicensingData
    {
        public LicensingState State { get; set; }
        public string TruncatedLicenseKey { get; set; }
        public int RemainingDemoDays { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class LicensableModuleAttribute : Attribute
    {
        public LicensableModuleAttribute()
        {
        }

        public bool HasSingleLicenseForAllStores { get; set; }
    }

    internal class LicensableModuleInfo
    {
        public string SystemName { get; set; }
        public Type ModuleType { get; set; }
        public bool IsLicensable { get; set; }
        public bool HasSingleLicenseForAllStores { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public sealed class LicenseRequiredAttribute : ActionFilterAttribute
    {
        public string ModuleSystemName { get; set; }
        public string MasterName { get; set; }
        public string ViewName { get; set; }
        public UnlicensedActionResult Result { get; set; }
        public bool BlockDemo { get; set; }
        public bool NotifyOnly { get; set; }
        public string NotificationMessage { get; set; }
        public string NotificationMessageType { get; set; }

        //public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        //{
        //    //throw new LicenseRequiredException(
        //    //    "Intrinsicly procrastinate frictionless intellectual capital through.",
        //    //    "Smartstore.AmazonPay",
        //    //    "/admin/payment/providers");

        //    var request = context?.HttpContext?.Request;
        //    if (request == null)
        //    {
        //        await next();
        //        return;
        //    }

        //    var controllerName = request.RouteValues.GetControllerName();
        //    var actionName = request.RouteValues.GetActionName();

        //    var model = new Dictionary<string, object>
        //    {
        //        { "SystemName", "Smartstore.AmazonPay" },
        //        { "ControllerName", controllerName },
        //        { "ActionName", actionName },
        //        { "LayoutName", "_Layout" },
        //        { "Message", "Intrinsicly procrastinate frictionless intellectual capital through." }
        //    };

        //    var controller = (Controller)context.Controller;
        //    context.Result = controller.View("LicenseRequired", model);
        //}

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Void
        }
    }

    //public sealed class LicenseRequiredException : SystemException
    //{
    //    public LicenseRequiredException(string message)
    //        : this(message.EmptyNull(), null)     // Do not pass null to avoid non-localized messages.
    //    {
    //    }

    //    public LicenseRequiredException(string message, string systemName = null, string returnUrl = null, Exception innerException = null)
    //        : base(message.EmptyNull(), innerException)
    //    {
    //        Data["SystemName"] = systemName;
    //        Data["ReturnUrl"] = returnUrl;
    //    }
    //}
}
