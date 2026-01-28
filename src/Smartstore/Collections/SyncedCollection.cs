#nullable enable

using System.Collections;
using Smartstore.Threading;

namespace Smartstore.Collections;

public sealed class SyncedCollection<T> : Disposable, ICollection<T>
{
    // INFO: Don't call it SynchronizedCollection because of framework dupe.
    private readonly ICollection<T> _col;
    private readonly ReaderWriterLockSlim _rwLock = new(LockRecursionPolicy.SupportsRecursion);

    public SyncedCollection(ICollection<T> wrappedCollection)
    {
        _col = Guard.NotNull(wrappedCollection);
    }

    public ReaderWriterLockSlim Lock
    {
        get => _rwLock;
    }

    public void AddRange(IEnumerable<T>? collection)
    {
        if (collection == null)
            return;

        using (_rwLock.GetWriteLock())
        {
            _col.AddRange(collection);
        }
    }

    public void Insert(int index, T item)
    {
        if (_col is IList<T> list)
        {
            using (_rwLock.GetWriteLock())
            {
                list.Insert(index, item);
            }
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public void InsertRange(int index, IEnumerable<T> values)
    {
        Guard.NotNull(values);

        if (_col is List<T> list)
        {
            using (_rwLock.GetWriteLock())
            {
                list.InsertRange(index, values);
            }
        }
        else if (_col is IList<T> ilist)
        {
            using (_rwLock.GetWriteLock())
            {
                foreach (var value in values)
                {
                    ilist.Insert(index++, value);
                }
            }
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public int RemoveRange(IEnumerable<T> values)
    {
        Guard.NotNull(values);

        int numRemoved = 0;

        using (_rwLock.GetWriteLock())
        {
            foreach (var value in values)
            {
                if (_col.Remove(value))
                {
                    numRemoved++;
                }
            }
        }

        return numRemoved;
    }

    public void RemoveRange(int index, int count)
    {
        if (_col is List<T> list)
        {
            using (_rwLock.GetWriteLock())
            {
                list.RemoveRange(index, count);
            }
        }
        else if (_col is IList<T> ilist)
        {
            using (_rwLock.GetWriteLock())
            {
                if ((uint)index > (uint)ilist.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (count < 0 || index + count > ilist.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                for (var i = 0; i < count; i++)
                {
                    ilist.RemoveAt(index);
                }
            }
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public void RemoveAt(int index)
    {
        using (_rwLock.GetWriteLock())
        {
            if (_col is IList<T> list)
            {
                list.RemoveAt(index);
                return;
            }

            // For non-indexable collections: emulate index-based semantics
            var snapshot = _col.ToList();

            if ((uint)index >= (uint)snapshot.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var item = snapshot[index];

            // Remove the element instance/value at that position (including null)
            _col.Remove(item);
        }
    }

    public T this[int index]
    {
        get
        {
            using (_rwLock.GetReadLock())
            {
                if (_col is IReadOnlyList<T> rolist)
                {
                    return rolist[index];
                }
                else if (_col is IList<T> list)
                {
                    return list[index];
                }
                else
                {
                    return _col.ElementAt(index);
                }
            }
        }
    }

    protected override void OnDispose(bool disposing)
    {
        if (disposing)
        {
            _rwLock.Dispose();
        }
    }

    #region ICollection<T>

    public int Count
    {
        get
        {
            using (_rwLock.GetReadLock())
            {
                return _col.Count;
            }
        }
    }

    public bool IsReadOnly => _col.IsReadOnly;

    public void Add(T item)
    {
        using (_rwLock.GetWriteLock())
        {
            _col.Add(item);
        }
    }

    public void Clear()
    {
        using (_rwLock.GetWriteLock())
        {
            _col.Clear();
        }
    }

    public bool Contains(T item)
    {
        using (_rwLock.GetReadLock())
        {
            return _col.Contains(item);
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Guard.NotNull(array);

        if (array.IsSynchronized)
        {
            // Have timeout in case of deadlock
            if (Monitor.TryEnter(array.SyncRoot, 1000))
            {
                try
                {
                    using (_rwLock.GetReadLock())
                    {
                        _col.CopyTo(array, arrayIndex);
                    }
                }
                finally
                {
                    Monitor.Exit(array.SyncRoot);
                }
            }
            else
            {
                throw new TimeoutException("Failed to copy to array.");
            }
        }
        else
        {
            using (_rwLock.GetReadLock())
            {
                _col.CopyTo(array, arrayIndex);
            }
        }
    }

    public bool Remove(T item)
    {
        using (_rwLock.GetWriteLock())
        {
            return _col.Remove(item);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerator<T> GetEnumerator()
    {
        // Create a collection snaphot
        List<T> snapshot;
        using (_rwLock.GetReadLock())
        {
            snapshot = _col.ToList();
        }

        return snapshot.GetEnumerator();
    }

    #endregion
}
