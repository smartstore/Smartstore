using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Caching;
using Smartstore.ComponentModel;
using Smartstore.Redis.Configuration;

namespace Smartstore.Redis
{
    public class RedisNativeJsonSerializer : IRedisSerializer
    {
        private static readonly byte[] NullResult = Encoding.UTF8.GetBytes("null");

        // Contains types that cannot be (de)serialized
        private readonly HashSet<Type> _unSerializableTypes = new() { typeof(Task), typeof(Task<>) };
        private readonly HashSet<Type> _unDeserializableTypes = new() { typeof(Task), typeof(Task<>) };

        private readonly RedisConfiguration _configuration;

        public RedisNativeJsonSerializer(RedisConfiguration configuration)
        {
            _configuration = configuration;
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

        public bool TrySerialize(object value, bool compress, out byte[] result)
        {
            result = null;

            if (!CanSerialize(value))
            {
                return false;
            }

            try
            {
                result = Serialize(value, compress);
                return true;
            }
            catch (Exception ex)
            {
                if (value != null)
                {
                    var t = GetInnerType(value);
                    _unSerializableTypes.Add(t);
                    Logger.Debug(ex, "Type '{0}' cannot be serialized", t);
                }

                return false;
            }
        }

        public bool TryDeserialize<T>(byte[] value, bool uncompress, out T result)
        {
            result = default;

            if (!CanDeserialize(typeof(T)))
            {
                return false;
            }

            try
            {
                result = Deserialize<T>(value, uncompress);
                return true;
            }
            catch (Exception ex)
            {
                var t = typeof(T);
                if (!(typeof(IObjectContainer).IsAssignableFrom(t) || t == typeof(object) || t.IsPredefinedType()))
                {
                    _unDeserializableTypes.Add(t);
                    Logger.Debug(ex, "Type '{0}' cannot be DEserialized", t);
                }

                return false;
            }
        }

        #region Private

        private T Deserialize<T>(byte[] value, bool uncompress)
        {
            return (T)Deserialize(typeof(T), value, uncompress);
        }

        private object Deserialize(Type objectType, byte[] value, bool uncompress)
        {
            Guard.NotNull(objectType, nameof(objectType));
            Guard.NotNull(value, nameof(value));

            // Check if null
            if (value.SequenceEqual(NullResult))
            {
                return null;
            }

            var options = new JsonSerializerOptions
            {
                //ContractResolver = SmartContractResolver.Instance,
                ////TypeNameHandling = TypeNameHandling.Objects,
                //ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                //ObjectCreationHandling = ObjectCreationHandling.Replace,
                //NullValueHandling = NullValueHandling.Ignore
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                //IgnoreReadOnlyProperties = true,
                IgnoreReadOnlyFields = true
            };

            if (!_configuration.DisableCompression && uncompress)
            {
                value = value.Unzip();
            }

            var json = Encoding.UTF8.GetString(value);
            return JsonSerializer.Deserialize(json, objectType, options);
        }

        private byte[] Serialize(object item, bool compress)
        {
            if (item == null)
            {
                return NullResult;
            }

            var options = new JsonSerializerOptions
            {
                //ContractResolver = SmartContractResolver.Instance,
                ////TypeNameHandling = TypeNameHandling.Objects,
                //ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                //NullValueHandling = NullValueHandling.Ignore
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                //IgnoreReadOnlyProperties = true,
                IgnoreReadOnlyFields = true
            };

            var buffer = JsonSerializer.SerializeToUtf8Bytes(item, item.GetType(), options);

            if (!_configuration.DisableCompression && compress)
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
            //if (objectType == null)
            //{
            //    return true;
            //}

            //if (set.Contains(objectType))
            //{
            //    return false;
            //}

            //if (objectType.IsGenericType)
            //{
            //    var gtDefinition = objectType.GetGenericTypeDefinition();
            //    if (set.Contains(gtDefinition))
            //    {
            //        return false;
            //    }
            //}

            return true;
        }

        private static Type GetInnerType(object obj)
        {
            if (obj is IObjectContainer wrapper)
            {
                return wrapper.ValueType ?? wrapper.Value?.GetType();
            }

            return obj?.GetType();
        }

        #endregion
    }
}