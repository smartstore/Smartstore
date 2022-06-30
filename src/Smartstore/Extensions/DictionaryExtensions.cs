using System.Dynamic;
using System.Runtime.CompilerServices;
using Smartstore.Utilities;

namespace Smartstore
{
    public static class DictionaryExtensions
    {
        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> values, IEnumerable<KeyValuePair<TKey, TValue>> other)
        {
            foreach (var kvp in other)
            {
                if (values.ContainsKey(kvp.Key))
                {
                    throw new ArgumentException("An item with the same key has already been added.");
                }
                values.Add(kvp);
            }
        }

        public static IDictionary<string, object> Merge(this IDictionary<string, object> instance, string key, object value, bool replaceExisting = true)
        {
            if (replaceExisting || !instance.ContainsKey(key))
            {
                instance[key] = value;
            }

            return instance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDictionary<string, object> Merge(this IDictionary<string, object> instance, object values, bool replaceExisting = true)
        {
            return instance.Merge(CommonHelper.ObjectToDictionary(values), replaceExisting);
        }

        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> instance, IDictionary<TKey, TValue> from, bool replaceExisting = true)
        {
            Guard.NotNull(instance, nameof(instance));
            Guard.NotNull(from, nameof(from));

            foreach (var kvp in from)
            {
                if (replaceExisting || !instance.ContainsKey(kvp.Key))
                {
                    instance[kvp.Key] = kvp.Value;
                }
            }

            return instance;
        }

        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> instance, TKey key, TValue value, bool replaceExisting = true)
        {
            Guard.NotNull(instance, nameof(instance));
            Guard.NotNull(key, nameof(key));

            if (replaceExisting || !instance.ContainsKey(key))
            {
                instance[key] = value;
            }

            return instance;
        }

        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> instance, TKey key, Func<TValue> valueAccessor, bool replaceExisting = true)
        {
            Guard.NotNull(instance, nameof(instance));
            Guard.NotNull(key, nameof(key));
            Guard.NotNull(valueAccessor, nameof(valueAccessor));

            if (replaceExisting || !instance.ContainsKey(key))
            {
                instance[key] = valueAccessor();
            }

            return instance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> instance, TKey key)
        {
            Guard.NotNull(instance, nameof(instance)).TryGetValue(key, out var val);
            return val;
        }

        public static ExpandoObject ToExpandoObject(this IDictionary<string, object> source, bool castIfPossible = false)
        {
            Guard.NotNull(source, nameof(source));

            if (castIfPossible && source is ExpandoObject)
            {
                return source as ExpandoObject;
            }

            var result = new ExpandoObject();
            result.AddRange(source);

            return result;
        }


        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, TValue value, bool updateIfExists = false)
        {
            if (source == null || key == null)
            {
                return false;
            }

            if (source.ContainsKey(key))
            {
                if (updateIfExists)
                {
                    source[key] = value;
                    return true;
                }
            }
            else
            {
                source.Add(key, value);
                return true;
            }

            return false;
        }

        public static bool TryRemove<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, out TValue value)
        {
            value = default;

            if (source != null && key != null && source.TryGetValue(key, out value))
            {
                source.Remove(key);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDictionary<string, string> AppendInValue(this IDictionary<string, string> instance, string key, char separator, string value)
        {
            return AddInValue(instance, key, separator, value, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDictionary<string, string> PrependInValue(this IDictionary<string, string> instance, string key, char separator, string value)
        {
            return AddInValue(instance, key, separator, value, true);
        }

        internal static IDictionary<string, string> AddInValue(this IDictionary<string, string> instance, string key, char separator, string value, bool prepend = false)
        {
            Guard.NotEmpty(key, nameof(key));

            value = value.Trim(separator);

            if (string.IsNullOrEmpty(value))
            {
                return instance;
            }

            if (!instance.TryGetValue(key, out var currentValue))
            {
                instance[key] = value;
            }
            else
            {
                if (TryAddInValue(value, currentValue, separator, prepend, out var mergedValue))
                {
                    instance[key] = mergedValue;
                }
            }

            return instance;
        }

        internal static bool TryAddInValue(string value, string currentValue, char separator, bool prepend, out string mergedValue)
        {
            mergedValue = null;

            if (currentValue.IsEmpty())
            {
                mergedValue = value;
            }
            else
            {
                currentValue = currentValue.Trim(separator);

                var manyCurrentValues = currentValue.Contains(separator);
                var manyValues = value.Contains(separator);

                if (!manyCurrentValues && !manyValues)
                {
                    if (value != currentValue)
                    {
                        mergedValue = prepend
                            ? value + separator + currentValue
                            : currentValue + separator + value;
                    }
                }
                else
                {
                    var currentValues = manyCurrentValues
                        ? currentValue.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        : new[] { currentValue };

                    var attemptedValues = manyValues
                        ? value.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        : new[] { value };

                    var isDirty = false;

                    for (var i = 0; i < attemptedValues.Length; i++)
                    {
                        var attemptedValue = attemptedValues[i];

                        if (!currentValues.Contains(attemptedValue))
                        {
                            if (prepend)
                            {
                                var newCurrentValues = new string[currentValues.Length + 1];
                                newCurrentValues[0] = attemptedValue;
                                Array.Copy(currentValues, 0, newCurrentValues, 1, currentValues.Length);
                                currentValues = newCurrentValues;
                            }
                            else
                            {
                                Array.Resize(ref currentValues, currentValues.Length + 1);
                                currentValues[^1] = attemptedValue;
                            }

                            isDirty = true;
                        }
                    }

                    if (isDirty)
                    {
                        mergedValue = string.Join(separator, currentValues);
                    }
                }
            }
            
            return mergedValue != null;
        }
    }

}
