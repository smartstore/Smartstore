#nullable enable

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Runtime.CompilerServices;
using Smartstore.Utilities;

namespace Smartstore;

public static class DictionaryExtensions
{
    extension<TKey, TValue>(IDictionary<TKey, TValue?> source) where TKey : notnull
    {
        /// <summary>
        /// Adds a key/value pair to the <paramref name="source"/> dictionary if the key does not already exist.
        /// </summary>
        public TValue? GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
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
        /// Tries to add the specified key and value to the dictionary.
        /// </summary>
        public bool TryAdd(TKey key, TValue value, bool updateIfExists)
        {
            if (source == null || key == null)
            {
                return false;
            }

            if (source is ConcurrentDictionary<TKey, TValue> concurrentDict)
            {
                return concurrentDict.TryAdd(key, value);
            }

            Guard.NotNull(source);

            if (updateIfExists)
            {
                source[key] = value;
                return true;
            }

            return source.TryAdd(key, value);
        }

        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue?>> other)
        {
            Guard.NotNull(source);
            Guard.NotNull(other);

            foreach (var kvp in other)
            {
                if (source.ContainsKey(kvp.Key))
                {
                    throw new ArgumentException("An item with the same key has already been added.");
                }

                source.Add(kvp);
            }
        }
    }

    extension<TKey, TValue>(IDictionary<TKey, TValue> source)
        where TKey : notnull
    {
        /// <summary>
        /// Attempts to remove and return the value with the specified key from the dictionary.
        /// </summary>
        public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue? value)
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

            Guard.NotNull(source);

            if (source.TryGetValue(key, out value))
            {
                source.Remove(key);
                return true;
            }

            return false;
        }

        public IDictionary<TKey, TValue> Merge(IDictionary<TKey, TValue> from, bool replaceExisting = true)
        {
            Guard.NotNull(source);
            Guard.NotNull(from);

            foreach (var kvp in from)
            {
                if (replaceExisting || !source.ContainsKey(kvp.Key))
                {
                    source[kvp.Key] = kvp.Value;
                }
            }

            return source;
        }

        public IDictionary<TKey, TValue> Merge(TKey key, TValue value, bool replaceExisting = true)
        {
            Guard.NotNull(source);
            Guard.NotNull(key);

            if (replaceExisting || !source.ContainsKey(key))
            {
                source[key] = value;
            }

            return source;
        }

        public IDictionary<TKey, TValue> Merge(TKey key, Func<TValue> valueAccessor, bool replaceExisting = true)
        {
            Guard.NotNull(source);
            Guard.NotNull(key);
            Guard.NotNull(valueAccessor);

            if (replaceExisting || !source.ContainsKey(key))
            {
                source[key] = valueAccessor();
            }

            return source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue? Get(TKey key)
        {
            return Guard.NotNull(source).TryGetValue(key, out var val) ? val : default;
        }
    }

    extension(IDictionary<string, object?> source)
    {
        public IDictionary<string, object?> Merge(string key, object? value, bool replaceExisting = true)
        {
            Guard.NotNull(source);

            if (replaceExisting || !source.ContainsKey(key))
            {
                source[key] = value;
            }

            return source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDictionary<string, object?> Merge(object? values, bool replaceExisting = true)
        {
            return source.Merge(ConvertUtility.ObjectToDictionary(values), replaceExisting);
        }

        public bool TryGetValueAs<TValue>(string key, [MaybeNullWhen(false)] out TValue? value)
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

        public bool TryGetAndConvertValue<TValue>(string key, [MaybeNullWhen(false)] out TValue? value)
        {
            Guard.NotNull(source);

            if (source.TryGetValue(key, out var obj) && ConvertUtility.TryConvert(obj, out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        public ExpandoObject ToExpandoObject(bool castIfPossible = false)
        {
            Guard.NotNull(source);

            if (castIfPossible && source is ExpandoObject expandoObj)
            {
                return expandoObj;
            }

            var result = new ExpandoObject();
            result.AddRange(source);

            return result;
        }
    }

    extension(IDictionary<string, string?> instance)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDictionary<string, string?> AppendInValue(string key, char separator, string value)
        {
            return instance.AddInValue(key, separator, value, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDictionary<string, string?> PrependInValue(string key, char separator, string value)
        {
            return instance.AddInValue(key, separator, value, true);
        }

        internal IDictionary<string, string?> AddInValue(string key, char separator, string value, bool prepend = false)
        {
            Guard.NotNull(instance);
            Guard.NotNull(value);
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
            else if (TryAddInValue(value, currentValue, separator, prepend, out var mergedValue))
            {
                instance[key] = mergedValue;
            }

            return instance;
        }
    }

    internal static bool TryAddInValue(string value, string? currentValue, char separator, bool prepend, [MaybeNullWhen(false)] out string? mergedValue)
    {
        mergedValue = null;

        if (string.IsNullOrWhiteSpace(currentValue))
        {
            // Quick check to handle empty or identical values
            mergedValue = value;
        }
        else
        {
            currentValue = currentValue.Trim(separator);

            var hasManyCurrentValues = currentValue.Contains(separator);
            var hasManyAttemptedValues = value.Contains(separator);

            if (!hasManyCurrentValues && !hasManyAttemptedValues)
            {
                // Quick check to handle single values on both sides
                if (!string.Equals(value, currentValue, StringComparison.Ordinal))
                {
                    mergedValue = prepend
                        ? value + separator + currentValue
                        : currentValue + separator + value;
                }
            }
            else
            {
                // Split the current values
                var currentValues = hasManyCurrentValues
                    ? currentValue.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    : [currentValue];

                // Split the new values
                var attemptedValues = hasManyAttemptedValues
                    ? value.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    : [value];

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
