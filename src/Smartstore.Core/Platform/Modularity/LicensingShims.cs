using System;
using System.Threading.Tasks;
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

    public static class LicenseChecker
    {
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
            => new LicenseCheckerResult { State = LicensingState.Licensed, Success = true, TruncatedLicenseKey = "SM-01234-56789" };

        public static Task<LicenseCheckerResult> CheckAsync(string systemName, string url = null)
            => Task.FromResult(new LicenseCheckerResult { State = LicensingState.Licensed, Success = true, TruncatedLicenseKey = "SM-01234-56789" });

        public static LicensingState CheckState(string systemName, string url = null)
            => LicensingState.Licensed;

        public static Task<LicensingState> CheckStateAsync(string systemName, string url = null)
            => Task.FromResult(LicensingState.Licensed);

        public static LicensingData GetLicense(string systemName, string url = null)
            => new LicensingData { State = LicensingState.Licensed, TruncatedLicenseKey = "SM-01234-56789" };

        public static Task<LicensingData> GetLicenseAsync(string systemName, string url = null)
            => Task.FromResult(new LicensingData { State = LicensingState.Licensed, TruncatedLicenseKey = "SM-01234-56789" });

        public static bool IsLicensableModule(IModuleDescriptor descriptor)
            => true;

        public static bool IsLicensableModule(IModuleDescriptor descriptor, out bool hasSingleLicenseForAllStores)
        {
            hasSingleLicenseForAllStores = false;
            return true;
        }

        public static LicenseCheckerResult ResetState(string systemName, string url = null)
            => new LicenseCheckerResult { State = LicensingState.Licensed, Success = true, TruncatedLicenseKey = "SM-01234-56789" };

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

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public sealed class LicenseRequiredAttribute : ActionFilterAttribute
    {
        public string PluginSystemName { get; set; }
        public string MasterName { get; set; }
        public string ViewName { get; set; }
        public bool EmptyResultWhenUnlicensed { get; set; }
        public bool BlockDemo { get; set; }
        public bool NotifyOnly { get; set; }
        public string NotificationMessage { get; set; }
        public string NotificationMessageType { get; set; }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Void
        }
    }
}
