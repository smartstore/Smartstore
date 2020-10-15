using System;

namespace Smartstore.Redis
{
    public interface IRedisSerializer
    {
        bool CanSerialize(object obj);
        bool CanDeserialize(Type objectType);

        bool TrySerialize(object value, bool zip, out byte[] result);
        bool TryDeserialize<T>(byte[] value, bool unzip, out T result);
    }
}