using System;
using System.Threading.Tasks;
using Smartstore.Threading;

namespace Smartstore.Redis
{
    public interface IRedisSerializer
    {
        bool CanSerialize(object obj);
        bool CanDeserialize(Type objectType);

        bool TrySerialize(object value, bool compress, out byte[] result);
        bool TryDeserialize<T>(byte[] value, bool uncompress, out T result);
    }
}