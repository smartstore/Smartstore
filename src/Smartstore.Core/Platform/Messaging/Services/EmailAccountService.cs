using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Messaging
{
    [Important]
    public partial class EmailAccountService : AsyncDbSaveHook<EmailAccount>, IEmailAccountService
    {
        private readonly SmartDbContext _db;
        private readonly EmailAccountSettings _emailAccountSettings;

        public EmailAccountService(SmartDbContext db, EmailAccountSettings emailAccountSettings)
        {
            _db = db;
            _emailAccountSettings = emailAccountSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        #region Hook 

        private string _hookErrorMessage;

        protected override async Task<HookResult> OnDeletingAsync(EmailAccount entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            var settingName = nameof(EmailAccountSettings) + '.' + nameof(EmailAccountSettings.DefaultEmailAccountId);

            if (await _db.Settings.AnyAsync(x => x.Name == settingName && x.Value == entity.Id.ToString(), cancelToken))
            {
                entry.ResetState();
                _hookErrorMessage = T("Admin.Configuration.EmailAccounts.CannotDeleteDefaultAccount", entity.Email.NaIfEmpty());
            }
            else if (await _db.EmailAccounts.CountAsync(cancelToken) == 1)
            {
                entry.ResetState();
                _hookErrorMessage = T("Admin.Configuration.EmailAccounts.CannotDeleteLastAccount", entity.Email.NaIfEmpty());
            }

            return HookResult.Ok;
        }

        protected override Task<HookResult> OnDeletedAsync(EmailAccount entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

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

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var deletedAccountIds = entries
                .Where(x => x.InitialState == EntityState.Deleted)
                .Select(x => x.Entity)
                .OfType<EmailAccount>()
                .Select(x => x.Id)
                .ToList();

            if (deletedAccountIds.Count > 0)
            {
                await _db.MessageTemplates
                    .Where(x => deletedAccountIds.Contains(x.EmailAccountId))
                    .ExecuteUpdateAsync(x => x.SetProperty(p => p.EmailAccountId, p => _emailAccountSettings.DefaultEmailAccountId), cancelToken);
            }
        }

        #endregion

        public virtual EmailAccount GetDefaultEmailAccount()
        {
            return _db.EmailAccounts.FindById(_emailAccountSettings.DefaultEmailAccountId, false)
                ?? _db.EmailAccounts.AsNoTracking().OrderBy(x => x.Id).FirstOrDefault();
        }
    }
}
