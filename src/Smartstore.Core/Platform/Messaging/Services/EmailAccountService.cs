using Smartstore.Core.Data;
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

        #region Hook 

        protected override async Task<HookResult> OnDeletingAsync(EmailAccount entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if ((await _db.EmailAccounts.CountAsync(cancellationToken: cancelToken)) == 1)
                throw new SmartException("You cannot delete this email account. At least one account is required.");

            return HookResult.Ok;
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
