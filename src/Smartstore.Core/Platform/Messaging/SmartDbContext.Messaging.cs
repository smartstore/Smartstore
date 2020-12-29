using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Messages;

namespace Smartstore.Core.Data
{
    public partial class SmartDbContext
    {
        public DbSet<Campaign> Campaigns { get; set; }

        public DbSet<EmailAccount> EmailAccounts { get; set; }

        public DbSet<MessageTemplate> MessageTemplates { get; set; }

        public DbSet<NewsLetterSubscription> NewsLetterSubscriptions { get; set; }

        public DbSet<QueuedEmail> QueuedEmails { get; set; }

        public DbSet<QueuedEmailAttachment> QueuedEmailAttachments { get; set; }
    }
}