using Smartstore.Domain;

namespace Smartstore.Templating
{
    public sealed class NullTemplateEngine : ITemplateEngine
    {
        public static NullTemplateEngine Instance { get; } = new NullTemplateEngine();

        public ITemplate Compile(string template)
        {
            return new NullTemplate(template);
        }

        public Task<string> RenderAsync(string source, object data, IFormatProvider formatProvider = null)
        {
            return Task.FromResult(source);
        }

        public ITestModel CreateTestModelFor(BaseEntity entity, string modelPrefix)
        {
            return new NullTestModel();
        }

        internal class NullTestModel : ITestModel
        {
            public string ModelName => "TestModel";
        }

        internal class NullTemplate : ITemplate
        {
            private readonly string _source;

            public NullTemplate(string source)
            {
                _source = source;
            }

            public string Source => _source;

            public Task<string> RenderAsync(object data, IFormatProvider formatProvider)
            {
                return Task.FromResult(_source);
            }
        }
    }
}
