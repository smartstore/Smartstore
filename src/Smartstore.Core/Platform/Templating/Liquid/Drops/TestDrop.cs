using DotLiquid;
using Smartstore.ComponentModel;

namespace Smartstore.Templating.Liquid
{
    internal class TestDrop : ITestModel, ILiquidizable, IIndexable, ISafeObject
    {
        private readonly BaseEntity _entity;
        private readonly Type _type;
        private readonly string _modelPrefix;

        public TestDrop(BaseEntity entity, string modelPrefix)
        {
            _entity = entity;
            _type = entity.GetType();

            if (modelPrefix.HasValue())
            {
                _modelPrefix = modelPrefix.EnsureEndsWith('.');
            }

            _modelPrefix ??= string.Empty;
        }

        public string ModelName 
            => _type.Name;

        public object GetWrappedObject()
            => _entity;

        public bool ContainsKey(object key)
            => true;

        public object this[object key]
        {
            get
            {
                object value = null;

                if (key is string name)
                {
                    var modelPrefix = _modelPrefix + name;
                    var fastProp = FastProperty.GetProperty(_type, name);
                    var pi = fastProp?.Property;

                    if (pi == null)
                    {
                        value = "{{ " + modelPrefix + " }}";
                    }
                    else if (pi.PropertyType.IsPredefinedType())
                    {
                        value = "{{ " + modelPrefix + " }}";
                    }
                    else if (fastProp.IsSequenceType)
                    {
                        var seqType = pi.PropertyType.GetGenericArguments()[0];
                        if (typeof(BaseEntity).IsAssignableFrom(seqType))
                        {
                            var testObj1 = new TestDrop((BaseEntity)Activator.CreateInstance(seqType), "it");
                            var testObj2 = new TestDrop((BaseEntity)Activator.CreateInstance(seqType), "it");
                            var list = new List<TestDrop>();
                            list.Add(testObj1);
                            list.Add(testObj2);
                            value = list;
                        }
                    }
                    else if (typeof(BaseEntity).IsAssignableFrom(pi.PropertyType))
                    {
                        value = new TestDrop((BaseEntity)Activator.CreateInstance(pi.PropertyType), modelPrefix);
                    }

                    //if (value is string s)
                    //{
                    //	value = "<span class='dtc-var{0}'>{1}</span>".FormatInvariant(invalid ? " invalid" : "", value);
                    //}
                }

                return value;
            }
        }

        public object ToLiquid()
            => this;
    }
}
