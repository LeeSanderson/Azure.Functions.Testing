// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics;

namespace Azure.Functions.Testing.Cli.Telemetry.PersistenceChannel;

// ReSharper disable IdentifierTypo
internal abstract class SnapshottingCollection<TItem, TCollection> : ICollection<TItem>
    where TCollection : class, ICollection<TItem>
{
    protected readonly TCollection Collection;
    protected TCollection? Snapshot;

    protected SnapshottingCollection(TCollection collection)
    {
        Debug.Assert(collection != null, "collection");
        Collection = collection;
    }

    public int Count => GetSnapshot().Count;

    public bool IsReadOnly => false;

    public void Add(TItem item)
    {
        lock (Collection)
        {
            Collection.Add(item);
            Snapshot = default;
        }
    }

    public void Clear()
    {
        lock (Collection)
        {
            Collection.Clear();
            Snapshot = default;
        }
    }

    public bool Contains(TItem item)
    {
        return GetSnapshot().Contains(item);
    }

    public void CopyTo(TItem[] array, int arrayIndex)
    {
        GetSnapshot().CopyTo(array, arrayIndex);
    }

    public bool Remove(TItem item)
    {
        lock (Collection)
        {
            bool removed = Collection.Remove(item);
            if (removed)
            {
                Snapshot = default(TCollection);
            }

            return removed;
        }
    }

    public IEnumerator<TItem> GetEnumerator()
    {
        return GetSnapshot().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    protected abstract TCollection CreateSnapshot(TCollection collection);

    protected TCollection GetSnapshot()
    {
        TCollection? localSnapshot = Snapshot;
        if (localSnapshot == null)
        {
            lock (Collection)
            {
                Snapshot = CreateSnapshot(Collection);
                localSnapshot = Snapshot;
            }
        }

        return localSnapshot;
    }
}
