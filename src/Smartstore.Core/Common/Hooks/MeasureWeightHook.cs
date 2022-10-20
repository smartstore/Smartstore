using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Common.Hooks
{
    internal class MeasureWeightHook : AsyncDbSaveHook<MeasureWeight>
    {
        private readonly MeasureSettings _measureSettings;
        private string _hookErrorMessage;

        public MeasureWeightHook(MeasureSettings measureSettings)
        {
            _measureSettings = measureSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        protected override Task<HookResult> OnDeletingAsync(MeasureWeight entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entity.Id == _measureSettings.BaseWeightId)
            {
                entry.ResetState();
                _hookErrorMessage = T("Admin.Configuration.Measures.Weights.CantDeletePrimary");
            }

            return Task.FromResult(HookResult.Ok);
        }

        public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_hookErrorMessage.HasValue())
            {
                var message = new string(_hookErrorMessage);
                _hookErrorMessage = null;

                throw new HookException(message);
            }

            return Task.CompletedTask;
        }
    }
}
