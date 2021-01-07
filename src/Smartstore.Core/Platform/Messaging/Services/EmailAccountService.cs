using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Messages
{
    [Important]
    public partial class EmailAccountService : AsyncDbSaveHook<EmailAccount>, IEmailAccountService
    {
        private readonly SmartDbContext _db;
        private readonly EmailAccountSettings _emailAccountSettings;

        public EmailAccountService(
            SmartDbContext db,
            EmailAccountSettings emailAccountSettings)
        {
            _db = db;
            _emailAccountSettings = emailAccountSettings;
        }

        #region Hook 

        protected override async Task<HookResult> OnDeletingAsync(EmailAccount entity, IHookedEntity entry, CancellationToken cancelToken)
        {
            if ((await _db.EmailAccounts.CountAsync()) == 1)
                throw new SmartException("You cannot delete this email account. At least one account is required.");

            return HookResult.Ok;
        }

        #endregion

        // TODO: (mh) (core) Don't forget model validation on insert
        public virtual async Task<EmailAccount> GetDefaultEmailAccountAsync()
        {
            var defaultEmailAccount = await _db.EmailAccounts
                .FindByIdAsync(_emailAccountSettings.DefaultEmailAccountId);

            if (defaultEmailAccount == null)
            {
                defaultEmailAccount = await _db.EmailAccounts
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
            }
        
            return defaultEmailAccount;
        }
    }
}
