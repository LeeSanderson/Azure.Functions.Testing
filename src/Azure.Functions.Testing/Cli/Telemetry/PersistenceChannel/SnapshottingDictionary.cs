﻿// ReSharper disable IdentifierTypo
using System.Diagnostics.CodeAnalysis;

namespace Azure.Functions.Testing.Cli.Telemetry.PersistenceChannel;

internal class SnapshottingDictionary<TKey, TValue> :
    SnapshottingCollection<KeyValuePair<TKey, TValue>, IDictionary<TKey, TValue>>, IDictionary<TKey, TValue> where TKey : notnull
{
    public SnapshottingDictionary()
        : base(new Dictionary<TKey, TValue>())
    {
    }

    public ICollection<TKey> Keys => GetSnapshot().Keys;

    public ICollection<TValue> Values => GetSnapshot().Values;

    public TValue this[TKey key]
    {
        get => GetSnapshot()[key];

        set
        {
            lock (Collection)
            {
                Collection[key] = value;
                Snapshot = null;
            }
        }
    }

    public void Add(TKey key, TValue value)
    {
        lock (Collection)
        {
            Collection.Add(key, value);
            Snapshot = null;
        }
    }

    public bool ContainsKey(TKey key)
    {
        return GetSnapshot().ContainsKey(key);
    }

    public bool Remove(TKey key)
    {
        lock (Collection)
        {
            bool removed = Collection.Remove(key);
            if (removed)
            {
                Snapshot = null;
            }

            return removed;
        }
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return GetSnapshot().TryGetValue(key, out value);
    }

    protected sealed override IDictionary<TKey, TValue> CreateSnapshot(IDictionary<TKey, TValue> collection)
    {
        return new Dictionary<TKey, TValue>(collection);
    }
}
