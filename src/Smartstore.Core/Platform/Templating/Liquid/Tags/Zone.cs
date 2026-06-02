using System.Text.RegularExpressions;
using DotLiquid;
using DotLiquid.Exceptions;
using DotLiquid.Util;
using Smartstore.Events;

namespace Smartstore.Templating.Liquid;

internal sealed class ZoneTagFactory : ITagFactory
{
    private readonly IEventPublisher _eventPublisher;

    public ZoneTagFactory(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }

    public string TagName => "zone";

    public Tag Create()
    {
        return new Zone { EventPublisher = _eventPublisher };
    }

    class Zone : Tag
    {
        private static readonly Regex Syntax = R.B(@"^({0})", DotLiquid.Liquid.QuotedFragment);

        private string _zoneName;

        public IEventPublisher EventPublisher { get; set; }

        public override void Initialize(string tagName, string markup, List<string> tokens)
        {
            Match syntaxMatch = Syntax.Match(markup);

            if (syntaxMatch.Success)
            {
                _zoneName = syntaxMatch.Groups[1].Value;
            }
            else
            {
                throw new SyntaxException("Syntax Error in 'zone' tag - Valid syntax: zone '[ZoneName]'.");
            }

            base.Initialize(tagName, markup, tokens);
        }

        public override void Render(Context context, TextWriter result)
        {
            var zoneName = (string)context[_zoneName] ?? _zoneName;

            if (zoneName.IsEmpty())
                return;

            var model = context.Environments.First();

            // Eagerly resolve TemplateName on the rendering thread to avoid concurrent
            // Dictionary access inside DotLiquid's Context when consumers call this property
            // during async/parallel event dispatch.
            var templateName = context["Context.TemplateName", false] as string;

            var evt = new TemplateZoneRenderingEvent(zoneName, model, templateName)
            {
                LiquidContext = context
            };

            EventPublisher.Publish(evt);

            if (evt.Snippets != null && evt.Snippets.Count > 0)
            {
                foreach (var snippet in evt.Snippets)
                {
                    if (snippet.Parse)
                    {
                        Template.Parse(snippet.Content)
                            .Render(result, new RenderParameters(context.FormatProvider) { LocalVariables = model });
                    }
                    else
                    {
                        result.Write(snippet.Content);
                    }
                }
            }
        }
    }
}