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
            if (entity.Id == _emailAccountSettings.DefaultEmailAccountId)
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

        #endregion

        public virtual EmailAccount GetDefaultEmailAccount()
        {
            var defaultEmailAccount = _db.EmailAccounts
                .FindById(_emailAccountSettings.DefaultEmailAccountId);

            if (defaultEmailAccount == null)
            {
                defaultEmailAccount = _db.EmailAccounts
                    .AsNoTracking()
                    .FirstOrDefault();
            }

            return defaultEmailAccount;
        }
    }
}
