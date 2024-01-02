using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Smartstore.Utilities;

namespace Smartstore.ComponentModel
{
    public class NewtonsoftJsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializer _jsonSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects });

        private static readonly byte[] NullResult = Encoding.UTF8.GetBytes("null");

        // Contains types that cannot be (de)serialized
        private readonly HashSet<Type> _unSerializableTypes = [typeof(Task), typeof(Task<>)];
        private readonly HashSet<Type> _unDeserializableTypes = [typeof(Task), typeof(Task<>)];

        //private static JsonSerializerSettings CreateSerializerSettings()
        //{
        //    var settings = new JsonSerializerSettings
        //    {
        //        ContractResolver = SmartContractResolver.Instance,
        //        TypeNameHandling = TypeNameHandling.Objects,
        //        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        //        ObjectCreationHandling = ObjectCreationHandling.Auto,
        //        NullValueHandling = NullValueHandling.Ignore,
        //        MaxDepth = 32
        //    };

        //    return settings;
        //}

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual bool CanSerialize(object obj)
        {
            return IsSerializableType(GetInnerType(obj), _unSerializableTypes);
        }

        public virtual bool CanSerialize(Type objectType)
        {
            return IsSerializableType(objectType, _unSerializableTypes);
        }

        public virtual bool CanDeserialize(Type objectType)
        {
            return IsSerializableType(objectType, _unDeserializableTypes);
        }

        public virtual bool TrySerialize(object value, bool compress, out byte[] result)
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

        public virtual bool TryDeserialize(Type objectType, byte[] value, bool uncompress, out object result)
        {
            Guard.NotNull(objectType);

            result = null;

            if (!CanDeserialize(objectType))
            {
                return false;
            }

            try
            {
                result = Deserialize(objectType, value, uncompress);
                return true;
            }
            catch
            {
                if (!(typeof(IObjectContainer).IsAssignableFrom(objectType) || objectType == typeof(object) || objectType.IsBasicOrNullableType()))
                {
                    _unDeserializableTypes.Add(objectType);
                    Logger.Debug("Type '{0}' cannot be DEserialized", objectType);
                }

                return false;
            }
        }

        #region Private

        private object Deserialize(Type objectType, byte[] value, bool uncompress)
        {
            Guard.NotNull(objectType);
            Guard.NotNull(value);

            // Check if null
            if (value.SequenceEqual(NullResult))
            {
                return null;
            }

            if (uncompress)
            {
                value = value.Unzip();
            }

            var json = Encoding.UTF8.GetString(value);
            using var reader = new StringReader(json);
            return _jsonSerializer.Deserialize(reader, objectType);
        }

        private byte[] Serialize(object item, bool compress)
        {
            if (item == null)
            {
                return NullResult;
            }

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            using var writer = new StringWriter(sb);

            _jsonSerializer.Serialize(writer, item);
            var buffer = sb.ToString().GetBytes();

            if (compress)
            {
                return buffer.Zip();
            }

            return buffer;
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
                if (set.Contains(objectType.GetGenericTypeDefinition()))
                {
                    return false;
                }
            }

            return true;
        }

        private static Type GetInnerType(object obj)
        {
            if (obj is IObjectContainer wrapper)
            {
                return wrapper.Value?.GetType();
            }

            return obj?.GetType();
        }

        #endregion
    }
}