#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Autofac;
using Autofac.Core;
using Microsoft.AspNetCore.Http;
using Smartstore.Engine;
using Smartstore.Json;

namespace Smartstore;

public static class HttpSessionExtensions
{
    /// <summary>
    /// Determines whether the specified key exists in the session state.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ContainsKey(this ISession? session, string key)
        => session?.TryGetValue(key, out _) == true;

    /// <summary>
    /// Retrieves an object of the specified reference type from the session using the provided key.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetObject<T>(this ISession? session, string key) where T : class
    {
        TryGetObject<T>(session, key, out var result);
        return result;
    }

    /// <summary>
    /// Retrieves an object of type T from the session by the specified key, or adds it using the provided acquirer
    /// function if it does not already exist.
    /// </summary>
    /// <remarks>If the object is not found in the session, the acquirer function is invoked to obtain
    /// the object, which is then stored in the session for future retrieval.</remarks>
    /// <param name="acquirer">A function that provides the object to add to the session if it does not already exist. Cannot be null.</param>
    public static T? GetOrAddObject<T>(this ISession session, string key, Func<T?> acquirer) where T : class
    {
        Guard.NotNull(session);
        Guard.NotNull(acquirer);

        if (!TryGetObject<T>(session, key, out var result))
        {
            result = acquirer();
            TrySetObject(session, key, result);
        }
        
        return result;
    }

    /// <summary>
    /// Attempts to retrieve and deserialize an object of type T from the session using the specified key.
    /// </summary>
    /// <remarks>Returns <see langword="false"/> if the session is null, the key does not exist, or
    /// deserialization fails. The method uses a JSON serializer to convert the stored data back to the specified
    /// type.</remarks>
    public static bool TryGetObject<T>(this ISession? session, string key, [MaybeNullWhen(false)] out T? result) where T : class
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
            result = (T)obj!;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to store the specified object in the session under the given key, or removes the entry if the value is
    /// null.
    /// </summary>
    /// <remarks>If serialization of the object fails, the method returns false and does not modify the
    /// session. Removing an entry is considered successful if the key existed and was removed.</remarks>
    /// <param name="value">The object to store in the session. If null, the method removes any existing entry associated with the specified
    /// key.</param>
    /// <returns>true if the object was successfully stored or removed from the session; otherwise, false.</returns>
    public static bool TrySetObject<T>(this ISession? session, string key, T? value) where T : class
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
            session.Set(key, data!);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to remove the value with the specified key from the session.
    /// </summary>
    /// <remarks>This method checks whether the specified key exists in the session before attempting to
    /// remove it. No action is taken if the key does not exist.</remarks>
    /// <returns>true if the key existed and was removed from the session; otherwise, false.</returns>
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
