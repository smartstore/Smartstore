using Smartstore;
using Smartstore.Net.Mail;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MailServiceCollectionExtensions
    {
        public static IServiceCollection AddMailKitMailSender(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));
            services.AddScoped<IMailSender, MailKitSender>();
            return services;
        }
    }
}
