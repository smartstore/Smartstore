using Newtonsoft.Json;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging.Events;
using Smartstore.Core.Stores;
using Smartstore.Events;
using Smartstore.Net.Mail;
using Smartstore.Templating;
using Smartstore.Utilities;

namespace Smartstore.Core.Messaging
{
    public partial class MessageFactory : IMessageFactory
    {
        const string LoremIpsum = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua.";

        private static readonly string[] _verifyOrderAgeTemplateNames = new[]
        {
            MessageTemplateNames.ShipmentSentCustomer,
            MessageTemplateNames.ShipmentDeliveredCustomer,
            MessageTemplateNames.OrderCompletedCustomer,
            MessageTemplateNames.OrderCancelledCustomer,
            MessageTemplateNames.OrderNoteAddedCustomer,
            MessageTemplateNames.ReturnRequestStatusChangedCustomer
        };

        private Dictionary<string, Func<Task<object>>> _testModelFactories;

        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly ITemplateEngine _templateEngine;
        private readonly ITemplateManager _templateManager;
        private readonly IMessageModelProvider _modelProvider;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly ILanguageService _languageService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IMediaService _mediaService;
        private readonly IEventPublisher _eventPublisher;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly OrderSettings _orderSettings;

        public MessageFactory(
            SmartDbContext db,
            ICommonServices services,
            ITemplateEngine templateEngine,
            ITemplateManager templateManager,
            IMessageModelProvider modelProvider,
            IQueuedEmailService queuedEmailService,
            ILanguageService languageService,
            IEmailAccountService emailAccountService,
            EmailAccountSettings emailAccountSettings,
            IMediaService mediaService,
            IEventPublisher eventPublisher,
            IStoreContext storeContext,
            IWorkContext workContext,
            OrderSettings orderSettings)
        {
            _db = db;
            _services = services;
            _templateEngine = templateEngine;
            _templateManager = templateManager;
            _modelProvider = modelProvider;
            _queuedEmailService = queuedEmailService;
            _languageService = languageService;
            _emailAccountService = emailAccountService;
            _emailAccountSettings = emailAccountSettings;
            _mediaService = mediaService;
            _eventPublisher = eventPublisher;
            _storeContext = storeContext;
            _workContext = workContext;
            _orderSettings = orderSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual async Task<CreateMessageResult> CreateMessageAsync(MessageContext messageContext, bool queue, params object[] modelParts)
        {
            Guard.NotNull(messageContext);

            modelParts ??= Array.Empty<object>();

            // Handle TestMode
            if (messageContext.TestMode && modelParts.Length == 0)
            {
                modelParts = await GetTestModelsAsync(messageContext);
            }

            ValidateMessageContext(messageContext, ref modelParts);

            // Create and assign model.
            var model = messageContext.Model = new TemplateModel();

            // Do not create message if the template does not exist, is not authorized or not active.
            if (messageContext.MessageTemplate == null)
            {
                return new CreateMessageResult { Model = model, MessageContext = messageContext };
            }

            // Add all global template model parts.
            await _modelProvider.AddGlobalModelPartsAsync(messageContext);

            // Add specific template models for passed parts.
            foreach (var part in modelParts)
            {
                if (model != null)
                {
                    await _modelProvider.AddModelPartAsync(part, messageContext);
                }
            }

            // Give implementors the chance to customize the final template model.
            await _eventPublisher.PublishAsync(new MessageModelCreatedEvent(messageContext, model));

            var messageTemplate = messageContext.MessageTemplate;
            var languageId = messageContext.Language.Id;

            // Render templates
            var to = await RenderEmailAddressAsync(messageTemplate.To, messageContext);
            var replyTo = await RenderEmailAddressAsync(messageTemplate.ReplyTo, messageContext, false);
            var bcc = await RenderTemplateAsync(messageTemplate.GetLocalized((x) => x.BccEmailAddresses, languageId), messageContext, false);

            var subject = await RenderTemplateAsync(messageTemplate.GetLocalized((x) => x.Subject, languageId), messageContext);
            ((dynamic)model).Email.Subject = subject;

            var body = await RenderBodyTemplateAsync(messageContext);

            // CSS inliner
            body = InlineCss(body, model);

            // Model tree
            var modelTree = _modelProvider.BuildModelTree(model);
            var modelTreeJson = JsonConvert.SerializeObject(modelTree, new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                TypeNameHandling = TypeNameHandling.Objects
            });

            if (modelTreeJson != messageTemplate.LastModelTree)
            {
                messageContext.MessageTemplate.LastModelTree = modelTreeJson;
                if (!messageTemplate.IsTransientRecord())
                {
                    _db.TryUpdate(messageContext.MessageTemplate);
                    await _db.SaveChangesAsync();
                }
            }

            // Create queued email from template
            var qe = new QueuedEmail
            {
                Priority = 5,
                From = messageContext.SenderMailAddress ?? messageContext.EmailAccount.ToMailAddress(),
                To = string.Join(';', to.Select(x => x.ToString())),
                Bcc = bcc,
                ReplyTo = replyTo == null ? null : string.Join(';', replyTo.Select(x => x.ToString())),
                Subject = subject,
                Body = body,
                CreatedOnUtc = DateTime.UtcNow,
                EmailAccountId = messageContext.EmailAccount.Id,
                SendManually = messageTemplate.SendManually
            };

            // Create and add attachments (if any).
            await CreateAttachmentsAsync(qe, messageContext);

            if (queue)
            {
                // Put to queue.
                await QueueMessageAsync(messageContext, qe);
            }

            return new CreateMessageResult { Email = qe, Model = model, MessageContext = messageContext };
        }

        public virtual async Task QueueMessageAsync(MessageContext messageContext, QueuedEmail queuedEmail)
        {
            Guard.NotNull(messageContext);
            Guard.NotNull(queuedEmail);

            // Publish event so that integrators can add attachments, alter the email etc.
            await _eventPublisher.PublishAsync(new MessageQueuingEvent
            {
                QueuedEmail = queuedEmail,
                MessageContext = messageContext,
                MessageModel = messageContext.Model
            });

            _db.QueuedEmails.Add(queuedEmail);
            await _db.SaveChangesAsync();
        }

        private async Task<List<MailAddress>> RenderEmailAddressAsync(string email, MessageContext ctx, bool required = true)
        {
            string parsed = null;

            try
            {
                parsed = await RenderTemplateAsync(email, ctx, required);

                if (required || parsed.HasValue())
                {
                    var parsedEmails = parsed
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(e => e.Convert<MailAddress>())
                        .Where(e => e != null)
                        .ToList();

                    return parsedEmails;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                if (ctx.TestMode)
                {
                    return new() { new("john@doe.com", "John Doe") };
                }

                var ex2 = new InvalidOperationException($"Failed to parse email address for variable '{email}'. Value was '{parsed.EmptyNull()}': {ex.Message}", ex);
                _services.Notifier.Error(ex2.Message);
                throw ex2;
            }
        }

        private Task<string> RenderTemplateAsync(string template, MessageContext ctx, bool required = true)
        {
            if (!required && template.IsEmpty())
            {
                return Task.FromResult<string>(null);
            }

            return _templateEngine.RenderAsync(template, ctx.Model, ctx.FormatProvider);
        }

        private async Task<string> RenderBodyTemplateAsync(MessageContext ctx)
        {
            var key = BuildTemplateKey(ctx);
            var source = ctx.MessageTemplate.GetLocalized((x) => x.Body, ctx.Language);
            var fromCache = true;
            var template = _templateManager.GetOrAdd(key, GetBodyTemplate);

            if (fromCache && template.Source != source)
            {
                // The template was resolved from template cache, but it has expired
                // because the source text has changed.
                template = _templateEngine.Compile(source);
                _templateManager.Put(key, template);
            }

            return await template.RenderAsync(ctx.Model, ctx.FormatProvider);

            string GetBodyTemplate()
            {
                fromCache = false;
                return source;
            }
        }

        private static string BuildTemplateKey(MessageContext messageContext)
        {
            var prefix = messageContext.MessageTemplate.IsTransientRecord() ? "TransientTemplate/" : "MessageTemplate/";
            return prefix + messageContext.MessageTemplate.Name + '/' + messageContext.Language.Id + "/Body";
        }

        private static string InlineCss(string html, dynamic model)
        {
            Uri baseUri = null;

            try
            {
                // 'Store' is a global model part, so we pretty can be sure it exists.
                baseUri = new Uri((string)model.Store.Url);
            }
            catch
            {
            }

            var pm = new PreMailer.Net.PreMailer(html, baseUri);
            var result = pm.MoveCssInline(true, "#ignore");
            return result.Html;
        }

        protected virtual async Task CreateAttachmentsAsync(QueuedEmail queuedEmail, MessageContext messageContext)
        {
            var messageTemplate = messageContext.MessageTemplate;
            var languageId = messageContext.Language.Id;

            // Create attachments if any.
            var fileIds = (new int?[]
                {
                    messageTemplate.GetLocalized(x => x.Attachment1FileId, languageId),
                    messageTemplate.GetLocalized(x => x.Attachment2FileId, languageId),
                    messageTemplate.GetLocalized(x => x.Attachment3FileId, languageId)
                })
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .Distinct()
                .ToArray();

            if (fileIds.Any())
            {
                var files = await _mediaService.GetFilesByIdsAsync(fileIds, MediaLoadFlags.AsNoTracking);
                foreach (var file in files)
                {
                    queuedEmail.Attachments.Add(new QueuedEmailAttachment
                    {
                        StorageLocation = EmailAttachmentStorageLocation.FileReference,
                        MediaFileId = file.Id,
                        Name = file.Name,
                        MimeType = file.MimeType
                    });
                }
            }
        }

        private void ValidateMessageContext(MessageContext ctx, ref object[] modelParts)
        {
            var t = ctx.MessageTemplate;
            if (t != null)
            {
                if (t.To.IsEmpty() || t.Subject.IsEmpty() || t.Name.IsEmpty())
                {
                    throw new InvalidOperationException("Message template validation failed because at least one of the following properties has not been set: Name, To, Subject.");
                }
            }

            if (ctx.StoreId.GetValueOrDefault() == 0)
            {
                ctx.Store = _storeContext.CurrentStore;
                ctx.StoreId = ctx.Store.Id;
            }
            else
            {
                ctx.Store = _storeContext.GetStoreById(ctx.StoreId.Value);
            }

            if (ctx.BaseUri == null)
            {
                ctx.BaseUri = ctx.Store.GetBaseUri();
            }

            if (ctx.LanguageId.GetValueOrDefault() == 0)
            {
                ctx.Language = _workContext.WorkingLanguage;
                ctx.LanguageId = ctx.Language.Id;
            }
            else
            {
                ctx.Language = _db.Languages
                    .AsNoTracking()
                    .FirstOrDefault(x => x.Id == ctx.LanguageId.Value);
            }

            EnsureLanguageIsActive(ctx);

            var parts = modelParts?.AsEnumerable() ?? Enumerable.Empty<object>();

            if (ctx.Customer == null)
            {
                // Try to move Customer from parts to MessageContext
                var customer = parts.OfType<Customer>().FirstOrDefault();
                if (customer != null)
                {
                    // Exclude the found customer from parts list
                    parts = parts.Where(x => !object.ReferenceEquals(x, customer));
                }

                ctx.Customer = customer ?? _workContext.CurrentCustomer;
            }

            if (ctx.Customer.IsSystemAccount)
            {
                throw new ArgumentException("Cannot create messages for system customer accounts.", nameof(ctx));
            }

            if (ctx.MessageTemplate == null)
            {
                if (ctx.MessageTemplateName.IsEmpty())
                {
                    throw new ArgumentException("'MessageTemplateName' must not be empty if 'MessageTemplate' is null.", nameof(ctx));
                }

                if (CreateMessage(ctx, parts))
                {
                    // INFO: tracked because entity is updated in CreateMessageAsync.
                    ctx.MessageTemplate = _db.MessageTemplates
                        .Where(x => x.Name == ctx.MessageTemplateName)
                        .ApplyStoreFilter(ctx.Store.Id)
                        .FirstOrDefault();

                    if (!ctx.TestMode && ctx.MessageTemplate != null && !ctx.MessageTemplate.IsActive)
                    {
                        ctx.MessageTemplate = null;
                    }
                }
            }

            if (ctx.EmailAccount == null && ctx.MessageTemplate != null)
            {
                ctx.EmailAccount = GetEmailAccountOfMessageTemplate(ctx.MessageTemplate, ctx.Language.Id);
            }

            // Sort parts: "IModelPart" instances must come first
            var bagParts = parts.OfType<IModelPart>();
            if (bagParts.Any())
            {
                parts = bagParts.Concat(parts.Except(bagParts));
            }

            modelParts = parts.Where(x => x != null).ToArray();
        }

        protected virtual bool CreateMessage(MessageContext ctx, IEnumerable<object> parts)
        {
            // Check orders that are too old for message sending.
            if (!ctx.TestMode
                && _orderSettings.MaxMessageOrderAgeInDays > 0
                && _verifyOrderAgeTemplateNames.Contains(ctx.MessageTemplateName))
            {
                Order order = parts.OfType<Order>().FirstOrDefault();
                if (order != null 
                    && order.OrderStatus >= OrderStatus.Complete
                    && order.CreatedOnUtc.AddDays(_orderSettings.MaxMessageOrderAgeInDays) < DateTime.UtcNow)
                {
                    var createdOnStr = _services.DateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc).ToHumanizedString(false);
                    Logger.Info(T("Admin.MessageTemplate.OrderTooOldForMessageInfo", ctx.MessageTemplateName, order.Id, createdOnStr));

                    return false;
                }
            }

            return true;
        }

        protected EmailAccount GetEmailAccountOfMessageTemplate(MessageTemplate messageTemplate, int languageId)
        {
            // Note that the email account to be used can be specified separately for each language, that's why we use GetLocalized here.
            var accountId = messageTemplate.GetLocalized(x => x.EmailAccountId, languageId);
            var account = (_db.EmailAccounts.FindById(accountId, false) 
                ?? _emailAccountService.GetDefaultEmailAccount()) 
                ?? throw new Exception(T("Common.Error.NoEmailAccount"));

            return account;
        }

        private void EnsureLanguageIsActive(MessageContext ctx)
        {
            var language = ctx.Language;

            if (language == null || !language.Published)
            {
                // Load any language from the specified store.
                language = _db.Languages
                    .AsNoTracking()
                    .FirstOrDefault(x => x.Id == _languageService.GetMasterLanguageId(ctx.StoreId.Value));
            }

            if (language == null || !language.Published)
            {
                // Load any language.
                language = _languageService.GetAllLanguages().FirstOrDefault();
            }

            ctx.Language = language ?? throw new Exception(T("Common.Error.NoActiveLanguage"));
        }

        #region TestModels

        public virtual async Task<object[]> GetTestModelsAsync(MessageContext messageContext)
        {
            var templateName = messageContext.MessageTemplate?.Name ?? messageContext.MessageTemplateName;

            if (_testModelFactories == null)
            {
                _testModelFactories = new Dictionary<string, Func<Task<object>>>(StringComparer.OrdinalIgnoreCase)
                {
                    { nameof(Product), () => GetRandomEntity<Product>(x => !x.Deleted && !x.IsSystemProduct && x.Visibility != ProductVisibility.Hidden && x.Published) },
                    { nameof(Customer), () => GetRandomEntity<Customer>(x => !x.Deleted && !x.IsSystemAccount && !string.IsNullOrEmpty(x.Email)) },
                    { nameof(Order), () => GetRandomEntity<Order>(x => !x.Deleted) },
                    { nameof(Shipment), () => GetRandomEntity<Shipment>(x => !x.Order.Deleted) },
                    { nameof(OrderNote), () => GetRandomEntity<OrderNote>(x => !x.Order.Deleted) },
                    { nameof(RecurringPayment), () => GetRandomEntity<RecurringPayment>(x => !x.Deleted) },
                    { nameof(NewsletterSubscription), () => GetRandomEntity<NewsletterSubscription>(x => true) },
                    { nameof(Campaign), () => GetRandomEntity<Campaign>(x => true) },
                    { nameof(ReturnRequest), () => GetRandomEntity<ReturnRequest>(x => true) },
                    { nameof(OrderItem), () => GetRandomEntity<OrderItem>(x => !x.Order.Deleted) },
                    { nameof(GiftCard), () => GetRandomEntity<GiftCard>(x => true) },
                    { nameof(ProductReview), () => GetRandomEntity<ProductReview>(x => !x.Product.Deleted && !x.Product.IsSystemProduct && x.Product.Visibility != ProductVisibility.Hidden && x.Product.Published) },
                    { nameof(WalletHistory), () => GetRandomEntity<WalletHistory>(x => true) }
                };
            }

            var modelNames = messageContext.MessageTemplate.ModelTypes.SplitSafe(',').Distinct().ToArray();
            var models = new Dictionary<string, object>();
            var result = new List<object>();

            foreach (var modelName in modelNames)
            {
                var model = await GetModelFromExpressionAsync(modelName, models, _testModelFactories);
                if (model != null)
                {
                    result.Add(model);
                }
            }

            // Some models are special.
            var isTransientTemplate = messageContext.MessageTemplate != null && messageContext.MessageTemplate.IsTransientRecord();

            if (!isTransientTemplate)
            {
                switch (templateName)
                {
                    case MessageTemplateNames.SystemContactUs:
                        result.Add(new NamedModelPart("Message")
                        {
                            ["Subject"] = "Test subject",
                            ["Message"] = LoremIpsum,
                            ["SenderEmail"] = "jane@doe.com",
                            ["SenderName"] = "Jane Doe"
                        });
                        break;
                    case MessageTemplateNames.ProductQuestion:
                        result.Add(new NamedModelPart("Message")
                        {
                            ["Message"] = LoremIpsum,
                            ["SenderEmail"] = "jane@doe.com",
                            ["SenderName"] = "Jane Doe",
                            ["SenderPhone"] = "123456789"
                        });
                        break;
                    case MessageTemplateNames.ShareProduct:
                        result.Add(new NamedModelPart("Message")
                        {
                            ["Body"] = LoremIpsum,
                            ["From"] = "jane@doe.com",
                            ["To"] = "john@doe.com",
                        });
                        break;
                    case MessageTemplateNames.ShareWishlist:
                        result.Add(new NamedModelPart("Wishlist")
                        {
                            ["PersonalMessage"] = LoremIpsum,
                            ["From"] = "jane@doe.com",
                            ["To"] = "john@doe.com",
                        });
                        break;
                    case MessageTemplateNames.NewVatSubmittedStoreOwner:
                        result.Add(new NamedModelPart("VatValidationResult")
                        {
                            ["Name"] = "VatName",
                            ["Address"] = "VatAddress"
                        });
                        break;
                    case MessageTemplateNames.SystemGeneric:
                        result.Add(new NamedModelPart("Generic")
                        {
                            ["Email"] = "john@doe.com",
                            ["Subject"] = "Subject",
                            ["Body"] = LoremIpsum
                        });
                        break;
                }
            }

            return result.ToArray();
        }

        private async Task<object> GetModelFromExpressionAsync(string expression, IDictionary<string, object> models, Dictionary<string, Func<Task<object>>> factories)
        {
            object currentModel = null;
            int dotIndex = 0;
            int len = expression.Length;
            bool bof = true;

            for (var i = 0; i < len; i++)
            {
                string token;
                if (expression[i] == '.')
                {
                    bof = false;
                    token = expression[..i];
                }
                else if (i == len - 1)
                {
                    // End reached
                    token = expression;
                }
                else
                {
                    continue;
                }

                if (!models.TryGetValue(token, out currentModel))
                {
                    if (bof)
                    {
                        // It's a simple dot-less expression where the token
                        // is actually the model name
                        currentModel = await ResolveTestModel(token);
                    }
                    else
                    {
                        // Sub-token, e.g. "Order.Customer"
                        // Get "Customer" part, this is our property name, NOT the model name
                        var propName = token[(dotIndex + 1)..];
                        // Get parent model "Order"
                        var parentModel = models.Get(token[..dotIndex]);
                        if (parentModel == null)
                            break;

                        if (parentModel is ITestModel)
                        {
                            // When the parent model is a test model, we need to create a random instance
                            // instead of using the property value (which is null/void in this case)
                            currentModel = await ResolveTestModel(propName);
                        }
                        else
                        {
                            // Get "Customer" property of Order
                            var pi = parentModel.GetType().GetProperty(propName);
                            if (pi != null)
                            {
                                // Get "Customer" value
                                var propValue = pi.GetValue(parentModel);
                                if (propValue != null)
                                {
                                    currentModel = propValue;
                                    //// Resolve logical model name...
                                    //var modelName = _modelProvider.ResolveModelName(propValue);
                                    //if (modelName != null)
                                    //{
                                    //	// ...and create the value
                                    //	currentModel = factories.Get(modelName)?.Invoke();
                                    //}
                                }
                            }
                        }
                    }

                    if (currentModel == null)
                        break;

                    // Put it in dict as e.g. "Order.Customer"
                    models[token] = currentModel;
                }

                if (!bof)
                {
                    dotIndex = i;
                }
            }

            return currentModel;

            async Task<object> ResolveTestModel(string modelName)
            {
                if (!factories.TryGetValue(modelName, out var factory))
                {
                    var e = new PreviewModelResolveEvent { ModelName = modelName };
                    await _eventPublisher.PublishAsync(e);

                    if (e.Result != null)
                    {
                        return e.Result;
                    }
                }

                if (factory != null)
                {
                    return await factory.Invoke();
                }

                // If no random entity exists in the database (e.g. RecurringPayment), then the associated parent model is
                // of type 'TestDrop' and the child model may not be resolved (e.g. RecurringPayment.InitialOrder).
                return null;
            }
        }

        private async Task<object> GetRandomEntity<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity, new()
        {
            var dbSet = _db.Set<T>();
            var query = dbSet.Where(predicate);

            // Determine how many entities match the given predicate.
            var count = await query.CountAsync();

            object result;

            if (count > 0)
            {
                // Fetch a random one.
                var skip = CommonHelper.GenerateRandomInteger(0, count);
                result = await query.OrderBy(x => x.Id).Skip(skip).FirstOrDefaultAsync();
            }
            else
            {
                // No entity matches the predicate. Provide a fallback test entity.
                var entity = Activator.CreateInstance<T>();

                if (entity is NewsletterSubscription subscription)
                {
                    // Campaign preview requires NewsletterSubscription entity.
                    subscription.NewsletterSubscriptionGuid = Guid.NewGuid();
                    subscription.Email = "john@doe.com";
                    subscription.Active = true;
                    subscription.CreatedOnUtc = DateTime.UtcNow;
                    subscription.WorkingLanguageId = _workContext.WorkingLanguage.Id;

                    result = entity;
                }
                else
                {
                    result = _templateEngine.CreateTestModelFor(entity, entity.GetEntityName());
                }
            }

            return result;
        }

        #endregion
    }
}
