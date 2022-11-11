using Smartstore.Core.Messaging;

namespace Smartstore.Web.Api.Controllers.OData
{
    /// <summary>
    /// The endpoint for operations on NewsLetterSubscription entity.
    /// </summary>
    public class NewsletterSubscriptionsController : WebApiController<NewsletterSubscription>
    {
        private readonly INewsletterSubscriptionService _newsletterSubscriptionService;

        public NewsletterSubscriptionsController(INewsletterSubscriptionService newsletterSubscriptionService)
        {
            _newsletterSubscriptionService = newsletterSubscriptionService;
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Promotion.Newsletter.Read)]
        public IQueryable<NewsletterSubscription> Get()
        {
            return Entities.AsNoTracking();
        }

        [HttpGet, ApiQueryable]
        [Permission(Permissions.Promotion.Newsletter.Read)]
        public SingleResult<NewsletterSubscription> Get(int key)
        {
            return GetById(key);
        }

        // INFO: there is no insert permission because a subscription is always created by customer.

        [HttpPost]
        [Permission(Permissions.Promotion.Newsletter.Update)]
        public async Task<IActionResult> Post([FromBody] NewsletterSubscription entity)
        {
            if (!entity.Email.IsEmail())
            {
                return BadRequest($"Provided email address {entity.Email} is invalid.");
            }

            return await PostAsync(entity, async () =>
            {               
                // TODO: message should be optional. Add parameter:
                //await _messageFactory.SendNewsletterSubscriptionActivationMessageAsync(entity, entity.WorkingLanguageId);

                await Db.SaveChangesAsync();
            });
        }


    }
}
