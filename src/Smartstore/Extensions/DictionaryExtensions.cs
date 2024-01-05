using System.Collections.Concurrent;
using System.Dynamic;
using System.Runtime.CompilerServices;
using Smartstore.Utilities;

namespace Smartstore
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Adds a key/value pair to the <paramref name="source"/> dictionary 
        /// if the key does not already exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="valueFactory">The function used to generate a value for the key</param>
        /// <returns>The value for the key. This will be either the existing value for the key if the
        /// key is already in the dictionary, or the new value for the key as returned by <paramref name="valueFactory"/>
        /// if the key was not in the dictionary.</returns>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, Func<TKey, TValue> valueFactory)
        {
            Guard.NotNull(source);
            Guard.NotNull(key);
            Guard.NotNull(valueFactory);

            if (source is ConcurrentDictionary<TKey, TValue> concurrentDict)
            {
                return concurrentDict.GetOrAdd(key, valueFactory);
            }

            if (!source.TryGetValue(key, out var value))
            {
                source[key] = value = valueFactory(key);
            }

            return value;
        }

        /// <summary>
        /// Tries to add the specified key and value to the <paramref name="source"/> dictionary.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be a null reference for reference types.</param>
        /// <returns>
        /// true if the key/value pair was added to the dictionary successfully; otherwise, false.
        /// </returns>
        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, TValue value, bool updateIfExists)
        {
            if (source == null || key == null)
            {
                return false;
            }

            if (source is ConcurrentDictionary<TKey, TValue> concurrentDict)
            {
                return concurrentDict.TryAdd(key, value);
            }

            if (updateIfExists)
            {
                source[key] = value;
                return true;
            }
            else
            {
                return source.TryAdd(key, value);
            }
        }

        /// <summary>
        /// Attempts to remove and return the value with the specified key from the <paramref name="source"/> dictionary.
        /// </summary>
        /// <param name="key">The key of the element to remove and return.</param>
        /// <param name="value">
        /// When this method returns, <paramref name="value"/> contains the object removed from the
        /// dictionary or the default value of <typeparamref name="TValue"/> if the operation failed.
        /// </param>
        /// <returns>true if an object was removed successfully; otherwise, false.</returns>
        public static bool TryRemove<TKey, TValue>(this IDictionary<TKey, TValue> source, TKey key, out TValue value)
        {
            value = default;

            if (source is null || key is null)
            {
                return false;
            }

            if (source is ConcurrentDictionary<TKey, TValue> concurrentDict)
            {
                return concurrentDict.TryRemove(key, out value);
            }

            if (source.TryGetValue(key, out value))
            {
                source.Remove(key);
                return true;
            }

            return false;
        }

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

        public static IDictionary<string, object> Merge(this IDictionary<string, object> source, string key, object value, bool replaceExisting = true)
        {
            if (replaceExisting || !source.ContainsKey(key))
            {
                source[key] = value;
            }

            return source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDictionary<string, object> Merge(this IDictionary<string, object> source, object values, bool replaceExisting = true)
        {
            return source.Merge(ConvertUtility.ObjectToDictionary(values), replaceExisting);
        }

        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> instance, IDictionary<TKey, TValue> from, bool replaceExisting = true)
        {
            Guard.NotNull(instance);
            Guard.NotNull(from);

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
            Guard.NotNull(instance);
            Guard.NotNull(key);

            if (replaceExisting || !instance.ContainsKey(key))
            {
                instance[key] = value;
            }

            return instance;
        }

        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(this IDictionary<TKey, TValue> instance, TKey key, Func<TValue> valueAccessor, bool replaceExisting = true)
        {
            Guard.NotNull(instance);
            Guard.NotNull(key);
            Guard.NotNull(valueAccessor);

            if (replaceExisting || !instance.ContainsKey(key))
            {
                instance[key] = valueAccessor();
            }

            return instance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> instance, TKey key)
        {
            Guard.NotNull(instance);
            return instance.TryGetValue(key, out var val) ? val : default;
        }

        public static bool TryGetValueAs<TValue>(this IDictionary<string, object> source, string key, out TValue value)
        {
            Guard.NotNull(source);

            if (source.TryGetValue(key, out var obj) && obj is TValue typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryGetAndConvertValue<TValue>(this IDictionary<string, object> source, string key, out TValue value)
        {
            Guard.NotNull(source);

            if (source.TryGetValue(key, out var obj) && ConvertUtility.TryConvert(obj, out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        public static ExpandoObject ToExpandoObject(this IDictionary<string, object> source, bool castIfPossible = false)
        {
            Guard.NotNull(source);

            if (castIfPossible && source is ExpandoObject)
            {
                return source as ExpandoObject;
            }

            var result = new ExpandoObject();
            result.AddRange(source);

            return result;
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
            Guard.NotEmpty(key);

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
