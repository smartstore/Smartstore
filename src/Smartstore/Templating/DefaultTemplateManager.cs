using System.Collections.Concurrent;

namespace Smartstore.Templating
{
    public partial class DefaultTemplateManager : ITemplateManager
    {
        private readonly ConcurrentDictionary<string, ITemplate> _templates;
        private readonly ITemplateEngine _engine;

        public DefaultTemplateManager(ITemplateEngine engine)
        {
            _templates = new ConcurrentDictionary<string, ITemplate>(StringComparer.OrdinalIgnoreCase);
            _engine = engine;
        }

        public IReadOnlyDictionary<string, ITemplate> All()
        {
            return _templates;
        }

        public bool Contains(string name)
        {
            Guard.NotEmpty(name);

            return _templates.ContainsKey(name);
        }

        public ITemplate Get(string name)
        {
            Guard.NotEmpty(name);

            _templates.TryGetValue(name, out var template);
            return template;
        }

        public void Put(string name, ITemplate template)
        {
            Guard.NotEmpty(name);
            Guard.NotNull(template);

            _templates[name] = template;
        }

        public ITemplate GetOrAdd(string name, Func<string> sourceFactory)
        {
            Guard.NotEmpty(name);
            Guard.NotNull(sourceFactory);

            return _templates.GetOrAdd(name, key =>
            {
                return _engine.Compile(sourceFactory());
            });
        }

        public bool TryRemove(string name, out ITemplate template)
        {
            Guard.NotEmpty(name);

            return _templates.TryRemove(name, out template);
        }

        public void Clear()
        {
            _templates.Clear();
        }
    }
}
