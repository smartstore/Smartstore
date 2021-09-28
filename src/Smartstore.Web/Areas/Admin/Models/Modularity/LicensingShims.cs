using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public static Task<LicenseCheckerResult> CheckAsync(string systemName, string url = null)
            => Task.FromResult(new LicenseCheckerResult { State = LicensingState.Licensed, Success = true });

        public static Task<LicensingState> CheckStateAsync(string systemName, string url = null)
            => Task.FromResult(LicensingState.Licensed);

        public static Task<LicensingData> GetLicenseAsync(string systemName, string url = null)
            => Task.FromResult(new LicensingData { State = LicensingState.Licensed });

        public static bool IsLicensableModule(IModuleDescriptor descriptor)
            => false;

        public static bool IsLicensableModule(IModuleDescriptor descriptor, out bool hasSingleLicenseForAllStores)
        {
            hasSingleLicenseForAllStores = false;
            return false;
        }

        public static Task<LicenseCheckerResult> ResetStateAsync(string systemName, string url = null)
            => Task.FromResult(new LicenseCheckerResult { State = LicensingState.Licensed, Success = true });
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
}
