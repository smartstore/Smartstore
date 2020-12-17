using Smartstore;
using Smartstore.Net.Mail;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MailServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a MailKit based mail service
        /// </summary>
        public static IServiceCollection AddMailKitMailService(this IServiceCollection services)
        {
            Guard.NotNull(services, nameof(services));
            services.AddScoped<IMailService, DefaultMailService>();
            return services;
        }
    }
}