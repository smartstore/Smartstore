#nullable enable

using System.Collections;
using System.Collections.Frozen;
using System.ComponentModel;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Smartstore.Utilities;

namespace Smartstore.ComponentModel;

public enum MemberOptMethod
{
    Allow,
    Disallow
}

/// <summary>
/// Class that provides extensible properties and methods to an
/// existing object when cast to dynamic. This
/// dynamic object stores 'extra' properties in a dictionary or
/// checks the actual properties of the instance passed via
/// constructor.
/// </summary>
[Serializable]
public class HybridExpando : DynamicObject, IDictionary<string, object?>, INotifyPropertyChanged
{
    private object? _instance;
    private Type? _instanceType;

    /// <summary>
    /// Adjusted property list for the wrapped instance type after white/black-list members has been applied.
    /// </summary>
    private IDictionary<string, FastProperty>? _instanceProps;

    /// <summary>
    /// String Dictionary that contains the extra dynamic values stored on this object/instance
    /// </summary>
    public Dictionary<string, object?> Properties = new();

    private readonly ISet<string>? _optMembers;
    private readonly MemberOptMethod _optMethod;
    private readonly bool _returnNullWhenFalsy;

    private PropertyChangedEventHandler? _propertyChanged;

    public HybridExpando(bool returnNullWhenFalsy = false)
    {
        _returnNullWhenFalsy = returnNullWhenFalsy;
    }

    public HybridExpando(object instance, bool returnNullWhenFalsy = false)
    {
        Guard.NotNull(instance);

        _returnNullWhenFalsy = returnNullWhenFalsy;
        Initialize(instance);
    }

    public HybridExpando(object instance, IEnumerable<string> optMembers, MemberOptMethod optMethod, bool returnNullWhenFalsy = false)
    {
        Guard.NotNull(instance);

        _returnNullWhenFalsy = returnNullWhenFalsy;
        Initialize(instance);

        _optMethod = optMethod;

        _optMembers = optMembers as ISet<string> ?? new HashSet<string>(optMembers);
    }

    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add => _propertyChanged += value;
        remove => _propertyChanged -= value;
    }

    protected void Initialize(object? instance)
    {
        _instance = instance;
        _instanceType = instance?.GetType();
        _instanceProps = null; // ensure re-evaluation if instance changes
    }

    protected object? WrappedObject => _instance;

    public override IEnumerable<string> GetDynamicMemberNames()
    {
        // Avoid LINQ (allocations) and avoid creating duplicates.
        // Also avoids ToArray() allocation if caller enumerates.
        return GetDynamicMemberNamesIterator();

        IEnumerable<string> GetDynamicMemberNamesIterator()
        {
            if (_instance == null)
            {
                foreach (var k in Properties.Keys)
                    yield return k;

                yield break;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var k in Properties.Keys)
            {
                if (seen.Add(k))
                    yield return k;
            }

            foreach (var k in InstanceProperties.Keys)
            {
                if (seen.Add(k))
                    yield return k;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool TryGetMember(GetMemberBinder binder, out object? result)
        => TryGetMemberCore(binder.Name, out result);

    protected virtual bool TryGetMemberCore(string name, out object? result)
    {
        var exists = Properties.TryGetValue(name, out result);

        if (!exists && _instance != null)
        {
            try
            {
                exists = GetProperty(_instance, name, out result);
            }
            catch
            {
            }
        }

        if (_returnNullWhenFalsy && result != null && !CommonHelper.IsTruthy(result))
        {
            result = null;
        }

        return exists;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool TrySetMember(SetMemberBinder binder, object? value)
        => TrySetMemberCore(binder.Name, value);

    protected virtual bool TrySetMemberCore(string name, object? value)
    {
        object? oldValue = null;
        var result = false;

        // Prefer a single dictionary lookup + assignment.
        if (Properties.TryGetValue(name, out oldValue))
        {
            Properties[name] = value;
            result = true;
        }

        if (!result && _instance != null)
        {
            try
            {
                result = SetProperty(_instance, name, value, out oldValue);
            }
            catch
            {
            }
        }

        if (!result)
        {
            Properties[name] = value;
        }

        var handler = _propertyChanged;
        if (handler != null && !Equals(value, oldValue))
        {
            handler(this, new PropertyChangedEventArgs(name));
        }

        return true;
    }

    public void Override(string name, object? value = null)
    {
        Guard.NotEmpty(name);

        Properties.TryGetValue(name, out var oldValue);
        Properties[name] = value;

        var handler = _propertyChanged;
        if (handler != null && !Equals(value, oldValue))
        {
            handler(this, new PropertyChangedEventArgs(name));
        }
    }

    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        if (_instance != null)
        {
            try
            {
                if (InvokeMethod(_instance, binder.Name, args, out result))
                {
                    return true;
                }
            }
            catch
            {
            }
        }

        result = null;
        return false;
    }

    protected bool GetProperty(object instance, string name, out object? result)
    {
        if (InstanceProperties.TryGetValue(name, out var fastProp))
        {
            result = fastProp.GetValue(instance);
            return true;
        }

        result = null;
        return false;
    }

    protected bool SetProperty(object instance, string name, object? value, out object? oldValue)
    {
        oldValue = null;

        if (InstanceProperties.TryGetValue(name, out var fastProp))
        {
            oldValue = fastProp.GetValue(instance);
            fastProp.SetValue(instance, value);
            return true;
        }

        return false;
    }

    protected bool InvokeMethod(object instance, string name, object?[]? args, out object? result)
    {
        // NOTE: still reflection-based. If this is hot, best fix is caching MethodInfo per name.
        var mi = _instanceType?.GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
        if (mi != null)
        {
            result = mi.Invoke(instance, args);
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Convenience method that provides a string Indexer 
    /// to the Properties collection AND the strongly typed
    /// properties of the object by name.
    /// 
    /// // dynamic
    /// exp["Address"] = "112 nowhere lane"; 
    /// // strong
    /// var name = exp["StronglyTypedProperty"] as string; 
    /// </summary>
    /// <remarks>
    /// The getter checks the Properties dictionary first
    /// then looks in PropertyInfo for properties.
    /// The setter checks the instance properties before
    /// checking the Properties dictionary.
    /// </remarks>
    public object? this[string key]
    {
        get
        {
            if (!TryGetMemberCore(key, out var result))
            {
                throw new KeyNotFoundException();
            }

            return result;
        }
        set => TrySetMemberCore(key, value);
    }

    public IEnumerable<KeyValuePair<string, object?>> GetProperties(bool includeInstanceProperties = false)
    {
        foreach (var kvp in Properties)
        {
            yield return kvp;
        }

        if (!includeInstanceProperties || _instance == null)
        {
            yield break;
        }

        foreach (var kvp2 in InstanceProperties)
        {
            // Avoid extra local/indirection, and use TryGetValue to prevent double hashing.
            if (!Properties.ContainsKey(kvp2.Key))
            {
                var prop = kvp2.Value;
                yield return new KeyValuePair<string, object?>(prop.Name, prop.GetValue(_instance));
            }
        }
    }

    private IDictionary<string, FastProperty> InstanceProperties
    {
        get
        {
            if (_instance == null)
            {
                return FrozenDictionary<string, FastProperty>.Empty;
            }

            var props = _instanceProps;
            if (props != null)
            {
                return props;
            }

            props = FastProperty.GetProperties(_instance.GetType()) as IDictionary<string, FastProperty>;

            if (_optMembers != null)
            {
                // Avoid LINQ allocations. Filter in one pass.
                var filtered = new Dictionary<string, FastProperty>(props!.Count, StringComparer.Ordinal);

                if (_optMethod == MemberOptMethod.Allow)
                {
                    foreach (var kvp in props!)
                    {
                        if (_optMembers.Contains(kvp.Key))
                        {
                            filtered.Add(kvp.Key, kvp.Value);
                        }
                    }
                }
                else
                {
                    foreach (var kvp in props!)
                    {
                        if (!_optMembers.Contains(kvp.Key))
                        {
                            filtered.Add(kvp.Key, kvp.Value);
                        }
                    }
                }

                props = filtered;
            }

            _instanceProps = props;
            return props!;
        }
    }

    public bool Contains(KeyValuePair<string, object?> item, bool includeInstanceProperties = false)
        => Contains(item.Key, includeInstanceProperties);

    public bool Contains(string propertyName, bool includeInstanceProperties = false)
        => Properties.ContainsKey(propertyName)
            || (includeInstanceProperties && InstanceProperties.ContainsKey(propertyName));

    #region IDictionary<string, object?>

    ICollection<string> IDictionary<string, object?>.Keys
    {
        get => GetKeysSnapshot();
    }

    ICollection<object?> IDictionary<string, object?>.Values
    {
        get => GetValuesSnapshot();
    }

    int ICollection<KeyValuePair<string, object?>>.Count
    {
        get => GetCountSnapshot();
    }

    bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => false;

    object? IDictionary<string, object?>.this[string key]
    {
        get => this[key];
        set => this[key] = value;
    }

    bool IDictionary<string, object?>.ContainsKey(string key) => Contains(key, includeInstanceProperties: true);

    void IDictionary<string, object?>.Add(string key, object? value) => throw new NotImplementedException();

    bool IDictionary<string, object?>.Remove(string key) => throw new NotImplementedException();

    public bool TryGetValue(string key, out object? value)
    {
        // Avoid double lookups + exceptions by using the internal core method.
        return TryGetMemberCore(key, out value);
    }

    void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item) => throw new NotImplementedException();

    void ICollection<KeyValuePair<string, object?>>.Clear() => throw new NotImplementedException();

    bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item)
        => TryGetMemberCore(item.Key, out var value) && Equals(value, item.Value);

    void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        => throw new NotImplementedException();

    bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item) => throw new NotImplementedException();

    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
        => GetProperties(true).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetProperties(true).GetEnumerator();

    #endregion

    // Snapshot helpers to honor IDictionary contract (ICollection) without LINQ allocations.
    private ICollection<string> GetKeysSnapshot()
    {
        var list = new List<string>(Properties.Count + (_instance != null ? InstanceProperties.Count : 0));

        foreach (var kvp in Properties)
            list.Add(kvp.Key);

        if (_instance != null)
        {
            foreach (var kvp in InstanceProperties)
            {
                if (!Properties.ContainsKey(kvp.Key))
                    list.Add(kvp.Key);
            }
        }

        return list.AsReadOnly();
    }

    private ICollection<object?> GetValuesSnapshot()
    {
        var list = new List<object?>(Properties.Count + (_instance != null ? InstanceProperties.Count : 0));

        foreach (var kvp in Properties)
            list.Add(kvp.Value);

        if (_instance != null)
        {
            foreach (var kvp in InstanceProperties)
            {
                if (!Properties.ContainsKey(kvp.Key))
                    list.Add(kvp.Value.GetValue(_instance));
            }
        }

        return list.AsReadOnly();
    }

    private int GetCountSnapshot()
    {
        if (_instance == null)
            return Properties.Count;

        var count = Properties.Count;
        foreach (var kvp in InstanceProperties)
        {
            if (!Properties.ContainsKey(kvp.Key))
                count++;
        }

        return count;
    }
}