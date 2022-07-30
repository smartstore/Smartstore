using Autofac;
using Autofac.Core;
using Microsoft.AspNetCore.Http;
using Smartstore.ComponentModel;
using Smartstore.Engine;

namespace Smartstore
{
    public static class HttpSessionExtensions
    {
        public static bool ContainsKey(this ISession session, string key)
        {
            return session?.Get(key) != null;
        }

        public static T GetObject<T>(this ISession session, string key) where T : class
        {
            TryGetObject<T>(session, key, out var result);
            return result;
        }

        public static bool TryGetObject<T>(this ISession session, string key, out T result) where T : class
        {
            result = default;

            var data = session?.Get(key);
            if (data == null)
            {
                return false;
            }

            var serializer = GetSerializer();

            if (serializer.TryDeserialize(typeof(T), data, false, out var obj))
            {
                result = (T)obj;
                return true;
            }

            return false;
        }

        public static bool TrySetObject<T>(this ISession session, string key, T value) where T : class
        {
            if (session == null)
            {
                return false;
            }

            if (value == default)
            {
                return TryRemove(session, key);
            }

            var serializer = GetSerializer();

            if (serializer.TrySerialize(value, false, out var data))
            {
                session.Set(key, data);
                return true;
            }

            return false;
        }

        public static bool TryRemove(this ISession session, string key)
        {
            if (session?.TryGetValue(key, out _) == true)
            {
                session.Remove(key);
                return true;
            }

            return false;
        }

        private static IJsonSerializer GetSerializer()
        {
            var serializer = EngineContext.Current.Application.Services.ResolveOptional<IJsonSerializer>();

            if (serializer == null)
            {
                throw new DependencyResolutionException($"No '{typeof(IJsonSerializer)}' implementation registered.");
            }

            return serializer;
        }
    }
}
