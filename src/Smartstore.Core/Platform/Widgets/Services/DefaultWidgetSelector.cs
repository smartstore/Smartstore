using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.OutputCache;
using Smartstore.Core.Stores;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Core.Widgets
{
    public class DefaultWidgetSelector : AsyncDbSaveHook<BaseEntity>, IWidgetSelector
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

        internal static Dictionary<string, string> LegacyWidgetNames { get; } = new()
        {
            { "body_start_html_tag_after", "start" },
            { "body_end_html_tag_before", "end" },
            { "head_html_tag", "start" }
        };

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly IRequestCache _requestCache;
        private readonly IWidgetProvider _widgetProvider;
        private readonly IWidgetService _widgetService;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IDisplayControl _displayControl;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public DefaultWidgetSelector(
            SmartDbContext db,
            ICacheManager cache,
            IRequestCache requestCache,
            IWidgetService widgetService, 
            IWidgetProvider widgetProvider,
            IWorkContext workContext,
            IStoreContext storeContext,
            IDisplayControl displayControl,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _cache = cache;
            _requestCache = requestCache;
            _widgetService = widgetService;
            _widgetProvider = widgetProvider;
            _workContext = workContext;
            _storeContext = storeContext;
            _displayControl = displayControl;
            _httpContextAccessor = httpContextAccessor;
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

        public async Task<IEnumerable<WidgetInvoker>> GetWidgetsAsync(string zone, object model = null)
        {
            Guard.NotEmpty(zone, nameof(zone));

            if (LegacyWidgetNames.ContainsKey(zone))
            {
                LegacyWidgetNames.TryGetValue(zone, out zone);
            }
            
            var storeId = _storeContext.CurrentStore.Id;
            var isPublicWidget = !_httpContextAccessor.HttpContext.Request.IsAdminArea();
            var widgets = Enumerable.Empty<WidgetInvoker>();

            #region Module Widgets

            if (isPublicWidget)
            {
                widgets = _widgetService.LoadActiveWidgetsByWidgetZone(zone, storeId)
                    .Select(x => x.Value.GetDisplayWidget(zone, model, storeId))
                    .Where(x => x != null);
            }

            #endregion

            #region Topic Widgets

            if (isPublicWidget)
            {
                // Get topic widgets from STATIC cache
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
                        if (zones != null && zones.Any())
                        {
                            foreach (var zone in zones)
                            {
                                var topicWidgetInvoker = new TopicWidgetInvoker(topicWidget);
                                map.Add(zone, topicWidgetInvoker);
                            }
                        }
                    }

                    return map;
                });

                if (topicsByZone.ContainsKey(zone))
                {
                    widgets = widgets.Concat(topicsByZone[zone]);
                }
            }

            #endregion

            #region Request scoped widgets (provided by IWidgetProvider)

            widgets = widgets.Concat(_widgetProvider.GetWidgets(zone));

            #endregion

            widgets = widgets
                .Distinct()
                .OrderBy(x => x.Prepend)
                .ThenBy(x => x.Order);

            return widgets;
        }
    }

    internal class TopicWidgetInvoker : ComponentWidgetInvoker
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
