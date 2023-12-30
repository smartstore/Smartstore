using System.Collections;
using System.Collections.Frozen;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Linq;
using Smartstore.Utilities;

namespace Smartstore.ComponentModel.TypeConverters
{
    internal class DictionaryTypeConverter<T> : DefaultTypeConverter where T : IDictionary<string, object>
    {
        public DictionaryTypeConverter()
            : base(typeof(object))
        {
        }

        public override bool CanConvertFrom(Type type)
        {
            // A dictionary can be created from JObject, POCO, and anonymous types
            return type == typeof(JObject)
                || type.IsPlainObjectType()
                || type.IsAnonymousType();
        }

        public override bool CanConvertTo(Type type)
        {
            // A dictionary can be converted to POCO types with default ctor.
            return type.IsPlainObjectType() && type.HasDefaultConstructor();
        }

        public override object ConvertFrom(CultureInfo culture, object value)
        {
            // Obj > Dict
            var dict = ConvertUtility.ObjectToDictionary(value);
            var to = typeof(T);

            if (to == typeof(RouteValueDictionary))
            {
                return new RouteValueDictionary(dict);
            }
            else if (to == typeof(Dictionary<string, object>))
            {
                return (Dictionary<string, object>)dict;
            }
            else if (to == typeof(ExpandoObject))
            {
                var expando = new ExpandoObject();
                expando.Merge(dict);
                return expando;
            }
            else if (to == typeof(HybridExpando))
            {
                var expando = new HybridExpando();
                expando.Merge(dict);
                return expando;
            }
            else if (to == typeof(FrozenDictionary<string, object>))
            {
                return dict.ToFrozenDictionary();
            }
            else
            {
                return dict;
            }
        }

        public override object ConvertTo(CultureInfo culture, string format, object value, Type to)
        {
            // Dict > Obj
            if (value is IDictionary<string, object> dict)
            {
                var target = Activator.CreateInstance(to);
                Populate(dict, target);
                return target;
            }

            return base.ConvertTo(culture, format, value, to);
        }

        private void Populate(IDictionary<string, object> source, object target, params object[] populated)
        {
            foreach (var kvp in FastProperty.GetProperties(target.GetType()))
            {
                var pi = kvp.Value.Property;

                if (source.TryGetValue(pi.Name, out var value))
                {
                    if (pi.PropertyType.IsAssignableFrom(value?.GetType()))
                    {
                        SetProperty(target, pi, value);
                    }
                    else if (value is IDictionary<string, object> dict && !pi.PropertyType.IsBasicType())
                    {
                        var nestedTarget = pi.GetValue(target);
                        if (nestedTarget == null && CanConvertTo(pi.PropertyType))
                        {
                            nestedTarget = Activator.CreateInstance(pi.PropertyType);
                        }

                        if (nestedTarget != null)
                        {
                            populated = populated.Concat(new object[] { target }).ToArray();
                            Populate(dict, nestedTarget, populated);
                            SetProperty(target, pi, nestedTarget);
                        }
                    }
                    else
                    {
                        SetProperty(target, pi, value);
                    }
                }
                else
                {
                    if (pi.PropertyType.IsSequenceType(out var elementType)
                        && !pi.PropertyType.IsDictionaryType()
                        && CanConvertTo(elementType))
                    {
                        SetProperty(target, pi, ConvertEnumerable(source, pi, elementType));
                    }
                }
            }
        }

        private static void SetProperty(object instance, PropertyInfo pi, object value)
        {
            if (!pi.CanWrite)
            {
                return;
            }

            if (ConvertUtility.TryConvert(value, pi.PropertyType, CultureInfo.CurrentCulture, out var converted))
            {
                pi.SetValue(instance, converted);
            }
        }

        private static object ConvertEnumerable(IDictionary<string, object> source, PropertyInfo enumerableProp, Type elemType)
        {
            // REVIEW: Dieser Code ist redundant mit DefaultModelBinder u.Ä.
            // Entweder ablösen oder eliminieren (vielleicht ist es ja in diesem Kontext notwendig!??!)

            var anyValuesFound = true;
            var index = 0;
            var elements = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elemType));
            var properties = FastProperty.GetProperties(elemType);

            while (anyValuesFound)
            {
                object curElement = null;
                anyValuesFound = false; // false until proven otherwise

                foreach (var kvp in properties)
                {
                    var pi = kvp.Value.Property;
                    var key = string.Format("{0}[{1}].{2}", enumerableProp.Name, index, pi.Name);

                    if (source.TryGetValue(key, out var value))
                    {
                        anyValuesFound = true;

                        if (curElement == null)
                        {
                            curElement = Activator.CreateInstance(elemType);
                            elements.Add(curElement);
                        }

                        SetProperty(curElement, pi, value);
                    }
                }

                index++;
            }

            // --> EnumerableConverter<T>.CreateSequenceActivator(Type)
            var createActivatorMethod = typeof(EnumerableConverter<>).MakeGenericType(elemType)
                .GetMethod("CreateSequenceActivator", BindingFlags.Static | BindingFlags.NonPublic);

            // --> Get activator func by reflection
            var activator = createActivatorMethod.Invoke(null, new object[] { enumerableProp.PropertyType });

            // --> Invoke activator func: activator.Invoke(elements)
            var result = activator.GetType().GetMethod("Invoke").Invoke(activator, new object[] { elements });

            return result;
        }
    }
}
