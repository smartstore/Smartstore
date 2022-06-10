namespace Smartstore.Core.Messaging
{
    public partial interface ICampaignService
    {
        /// <summary>
        /// Sends a campaign newsletter to newsletter subscribers.
        /// </summary>
        /// <returns>Number of queued messages.</returns>
        Task<int> SendCampaignAsync(Campaign campaign, CancellationToken cancelToken = default);

        /// <summary>
        /// Creates a campaign message for the specified subscriber.
        /// Caller is responsible for database commit.
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
