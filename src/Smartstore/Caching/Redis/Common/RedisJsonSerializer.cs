using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Smartstore.Caching;
using Smartstore.ComponentModel;
using StackExchange.Redis;

namespace Smartstore.Redis
{
    public class RedisJsonSerializer : IRedisSerializer
    {
        // Contains types that cannot be (de)serialized
        private readonly HashSet<Type> _unSerializableTypes = new HashSet<Type> { typeof(Task), typeof(Task<>) };
        private readonly HashSet<Type> _unDeserializableTypes = new HashSet<Type> { typeof(Task), typeof(Task<>) };

        protected static readonly IDictionary<Type, string> TypeShortcutMap;

        protected static readonly IDictionary<string, Type> ShortcutTypeMap;

        /// <summary>
        /// Format string used to write type information into the Redis entry before the JSON data
        /// </summary>
        protected static readonly string TypeInfoPattern = "<t:{0}>";

        /// <summary>
        /// Regular expression used to extract type information from Redis entry
        /// </summary>
        protected static readonly Regex TypeInfoRegEx = new Regex(@"\<t\:(.*)\>", RegexOptions.Compiled);

        private static readonly byte[] NullResult = Encoding.UTF8.GetBytes("null");

        static RedisJsonSerializer()
        {
            // Internal dictionary for type info for commonly used types, which allows for slightly shorter
            // type names in the serialized output
            var map = new Dictionary<Type, string>()
            {
                { typeof(bool), "SysBool" },
                { typeof(byte), "SysByte" },
                { typeof(char), "SysChar" },
                { typeof(DateTime), "SysDateTime" },
                { typeof(decimal), "SysDecimal" },
                { typeof(double), "SysDouble" },
                { typeof(short), "SysShort" },
                { typeof(int), "SysInt" },
                { typeof(long), "SysLong" },
                { typeof(sbyte), "SysSByte" },
                { typeof(float), "SysFloat" },
                { typeof(string), "SysString" },
                { typeof(ushort), "SysUShort" },
                { typeof(uint), "SysUInt" },
                { typeof(ulong), "SysULong" },
                { typeof(TimeSpan), "SysTimeSpan" },
                { typeof(Guid), "SysGuid" }
            };

            TypeShortcutMap = new ReadOnlyDictionary<Type, string>(map);

            // the other way round
            ShortcutTypeMap = new ReadOnlyDictionary<string, Type>(map.ToDictionary(x => x.Value, x => x.Key));
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public bool CanSerialize(object obj)
        {
            return IsSerializableType(GetInnerType(obj), _unSerializableTypes);
        }

        public bool CanDeserialize(Type objectType)
        {
            return IsSerializableType(objectType, _unDeserializableTypes);
        }

        public bool TrySerialize(object value, bool zip, out byte[] result)
        {
            result = null;

            if (!CanSerialize(value))
            {
                return false;
            }

            try
            {
                result = Serialize(value, zip);
                return true;
            }
            catch
            {
                if (value != null)
                {
                    var t = GetInnerType(value);
                    _unSerializableTypes.Add(t);
                    Logger.Debug("Type '{0}' cannot be serialized", t);
                }

                return false;
            }
        }

        public bool TryDeserialize<T>(byte[] value, bool unzip, out T result)
        {
            result = default;

            if (!CanDeserialize(typeof(T)))
            {
                return false;
            }

            try
            {
                result = Deserialize<T>(value, unzip);
                return true;
            }
            catch
            {
                var t = typeof(T);
                if (!(typeof(IObjectWrapper).IsAssignableFrom(t) || t == typeof(object) || t.IsPredefinedType()))
                {
                    _unDeserializableTypes.Add(t);
                    Logger.Debug("Type '{0}' cannot be DEserialized", t);
                }

                return false;
            }
        }

        #region Private

        private static T Deserialize<T>(byte[] value, bool unZip)
        {
            return (T)Deserialize(typeof(T), value, unZip);
        }

        private static object Deserialize(Type objectType, byte[] value, bool unzip)
        {
            Guard.NotNull(objectType, nameof(objectType));
            Guard.NotNull(value, nameof(value));

            // Check if null
            if (value.SequenceEqual(NullResult))
            {
                return null;
            }

            // Check if predefined system/simple type
            var redisValue = (string)((RedisValue)value);
            var typeMatch = TypeInfoRegEx.Match(redisValue);
            if (typeMatch.Success)
            {
                var typeShortCut = typeMatch.Groups[1].Value;
                if (ShortcutTypeMap.TryGetValue(typeShortCut, out var t))
                {
                    return JsonConvert.DeserializeObject(redisValue[typeMatch.Length..], t);
                }
            }

            var settings = new JsonSerializerSettings
            {
                ContractResolver = SmartContractResolver.Instance,
                TypeNameHandling = TypeNameHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                NullValueHandling = NullValueHandling.Ignore
            };

            if (unzip)
            {
                value = value.UnZip();
            }

            var json = Encoding.UTF8.GetString(value);
            return JsonConvert.DeserializeObject(json, objectType, settings);
        }

        private static byte[] Serialize(object item, bool zip)
        {
            if (item == null)
            {
                return NullResult;
            }

            if (TypeShortcutMap.TryGetValue(item.GetType(), out var typeShortCut))
            {
                // !t:SysInt!1234
                var value = TypeInfoPattern.FormatInvariant(typeShortCut) + JsonConvert.SerializeObject(item);
                return Encoding.UTF8.GetBytes(value);
            }

            var settings = new JsonSerializerSettings
            {
                ContractResolver = SmartContractResolver.Instance,
                TypeNameHandling = TypeNameHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            var json = JsonConvert.SerializeObject(item, settings);

            var buffer = Encoding.UTF8.GetBytes(json);

            if (zip)
            {
                return buffer.Zip();
            }
            else
            {
                return buffer;
            }
        }


        private static bool IsSerializableType(Type objectType, HashSet<Type> set)
        {
            if (objectType == null)
            {
                return true;
            }

            if (set.Contains(objectType))
            {
                return false;
            }

            if (objectType.IsGenericType)
            {
                var gtDefinition = objectType.GetGenericTypeDefinition();
                if (set.Contains(gtDefinition))
                {
                    return false;
                }
            }

            return true;
        }

        private static Type GetInnerType(object obj)
        {
            if (obj is IObjectWrapper wrapper)
            {
                return wrapper.Value?.GetType();
            }

            return obj?.GetType();
        }

        #endregion
    }
}