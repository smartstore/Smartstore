using System.Threading.Tasks;

namespace Smartstore.Core.Messages
{
    public partial interface ICampaignService
    {
        /// <summary>
        /// Sends a campaign to all newsletter subscribers.
        /// </summary>
        /// <returns>Number of queued messages.</returns>
        Task<int> SendCampaignAsync(Campaign campaign);

        /// <summary>
        /// Sends a campaign to specified subscriber.
        /// </summary>
        /// <param name="campaign">Campaign.</param>
        /// <param name="subscriber">Newsletter subscriber.</param>
        /// <returns>Message result.</returns>
        Task<CreateMessageResult> CreateCampaignMessageAsync(Campaign campaign, NewsletterSubscriber subscriber);

        /// <summary>
        /// Creates a campaign email without sending it for previewing and testing purposes.
        /// </summary>
        /// <param name="campaign">The campaign to preview</param>
        /// <returns>The preview result.</returns>
        Task<CreateMessageResult> PreviewAsync(Campaign campaign);
    }
}
