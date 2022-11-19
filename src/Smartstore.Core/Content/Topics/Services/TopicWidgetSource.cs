using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Core.Widgets;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Content.Topics
{
    public class TopicWidgetSource : AsyncDbSaveHook<BaseEntity>, IWidgetSource
    {
        const string ByZoneTopicsCacheKey = "Smartstore.TopicWidgets.ZoneMapped";

        /// <summary>
        /// Key for TopicWidget caching
        /// </summary>
        /// <remarks>
        /// {0} : store id
        /// {1} : language id
        /// {2} : role ids
        /// </remarks>
        const string TOPIC_WIDGET_ALL_MODEL_KEY = "pres:topic:widget-all-{0}-{1}-{2}";
        const string TOPIC_WIDGET_PATTERN_KEY = "pres:topic:widget*";

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly IRequestCache _requestCache;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;

        public TopicWidgetSource(
            SmartDbContext db,
            ICacheManager cache,
            IRequestCache requestCache,
            IWorkContext workContext,
            IStoreContext storeContext)
        {
            _db = db;
            _cache = cache;
            _requestCache = requestCache;
            _workContext = workContext;
            _storeContext = storeContext;
        }

        #region Hook

        public override async Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            var result = HookResult.Ok;

            if (entry.Entity is Topic)
            {
                await _cache.RemoveByPatternAsync(TOPIC_WIDGET_PATTERN_KEY);
            }
            else if (entry.Entity is LocalizedProperty lp)
            {
                if (lp.LocaleKeyGroup == nameof(Topic))
                {
                    await _cache.RemoveByPatternAsync(TOPIC_WIDGET_PATTERN_KEY);
                }
            }
            else
            {
                // Register as void hook for all other entity types
                result = HookResult.Void;
            }

            return result;
        }

        #endregion

        public int Order => 0;

        public async Task<IEnumerable<Widget>> GetWidgetsAsync(string zone, bool isPublicArea, object model = null)
        {
            if (!isPublicArea)
            {
                return null;
            }

            // Get topic widgets from STATIC cache
            var storeId = _storeContext.CurrentStore.Id;
            var allTopicsCacheKey = string.Format(TOPIC_WIDGET_ALL_MODEL_KEY, storeId, _workContext.WorkingLanguage.Id, _workContext.CurrentCustomer.GetRolesIdent());

            var topicWidgets = await _cache.GetAsync(allTopicsCacheKey, async () =>
            {
                var allTopicWidgets = await _db.Topics
                    .AsNoTracking()
                    .ApplyStandardFilter(customerRoleIds: _workContext.CurrentCustomer.GetRoleIds(), storeId: storeId)
                    .Where(x => x.RenderAsWidget)
                    .ToListAsync();

                var stubs = allTopicWidgets
                    .Select(t =>
                    {
                        var locTitle = t.GetLocalized(x => t.Title);
                        var locBody = t.GetLocalized(x => t.Body, detectEmptyHtml: false);

                        return new TopicWidget
                        {
                            Id = t.Id,
                            Bordered = t.WidgetBordered,
                            WrapContent = !t.WidgetWrapContent.HasValue || t.WidgetWrapContent.Value,
                            ShowTitle = t.WidgetShowTitle,
                            SystemName = t.SystemName.SanitizeHtmlId(),
                            ShortTitle = t.GetLocalized(x => x.ShortTitle),
                            Title = locTitle,
                            TitleRtl = locTitle.CurrentLanguage.Rtl,
                            Intro = t.GetLocalized(x => x.Intro),
                            Body = locBody,
                            BodyRtl = locBody.CurrentLanguage.Rtl,
                            TitleTag = t.TitleTag,
                            WidgetZones = t.GetWidgetZones().ToArray(),
                            Priority = t.Priority,
                            CookieType = t.CookieType
                        };
                    })
                    .OrderBy(t => t.Priority)
                    .ToList();

                return stubs;
            });

            // Save widgets to zones map in request cache
            var topicsByZone = _requestCache.Get(ByZoneTopicsCacheKey, () =>
            {
                var map = new Multimap<string, TopicWidgetInvoker>(StringComparer.OrdinalIgnoreCase);

                foreach (var topicWidget in topicWidgets)
                {
                    var zones = topicWidget.WidgetZones;
                    if (zones != null)
                    {
                        for (var i = 0; i < zones.Length; i++)
                        {
                            var topicWidgetInvoker = new TopicWidgetInvoker(topicWidget);
                            map.Add(zones[i], topicWidgetInvoker);
                        }
                    }
                }
                return map;
            });

            if (topicsByZone.TryGetValues(zone, out var values))
            {
                return values;
            }

            return null;
        }
    }

    internal class TopicWidgetInvoker : ComponentWidget
    {
        public TopicWidgetInvoker(TopicWidget model)
            : base("TopicWidget", new { model = Guard.NotNull(model, nameof(model)) })
        {
            // TODO: (core) TopicWidgetInvoker: I don't like the fact that a core library calls into web frontend.
            // Maybe we can granularize this part and move it to web project.

            Model = model;
        }

        public TopicWidget Model { get; }
    }

    public class TopicWidget
    {
        public int Id { get; set; }
        public string[] WidgetZones { get; set; }
        public string SystemName { get; set; }
        public bool WrapContent { get; set; }
        public bool ShowTitle { get; set; }
        public bool Bordered { get; set; }
        public string ShortTitle { get; set; }
        public string Title { get; set; }
        public string Intro { get; set; }
        public string Body { get; set; }
        public bool TitleRtl { get; set; }
        public bool BodyRtl { get; set; }

        public string TitleTag { get; set; }
        public int Priority { get; set; }
        public CookieType? CookieType { get; set; }
    }
}
