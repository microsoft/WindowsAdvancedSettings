// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;

namespace FileExplorerGitIntegration.Models;

// A simple LRU cache dictionary implementation.
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "File names for generics are ugly.")]
internal sealed class LruCacheDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly int _capacity;
    private const int DefaultCapacity = 512;
    private readonly object _lock = new();
    private readonly Dictionary<TKey, LinkedListNode<(TKey, TValue)>> _dict;
    private readonly LinkedList<(TKey, TValue)> _lruList = new();

    public LruCacheDictionary(int capacity = DefaultCapacity)
    {
        _capacity = capacity;
        _dict = [];
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        lock (_lock)
        {
            if (_dict.TryGetValue(key, out var node))
            {
                RenewExistingNodeNoLock(node);
                value = node.Value.Item2;
                return true;
            }

            value = default!;
            return false;
        }
    }

    public TValue GetOrAdd(TKey key, TValue value)
    {
        lock (_lock)
        {
            if (_dict.TryGetValue(key, out var node))
            {
                RenewExistingNodeNoLock(node);
                return node.Value.Item2;
            }

            AddAndTrimNoLock(key, value);
            return value;
        }
    }

    private void AddAndTrimNoLock(TKey key, TValue value)
    {
        var newNode = new LinkedListNode<(TKey, TValue)>((key, value));
        AddNewNodeNoLock(newNode);
        TrimToCapacityNoLock();
    }

    private void RenewExistingNodeNoLock(LinkedListNode<(TKey, TValue)> node)
    {
        Debug.Assert(node.List == _lruList, "Node is not in the list");
        _lruList.Remove(node);
        _lruList.AddLast(node);
    }

    private void AddNewNodeNoLock(LinkedListNode<(TKey, TValue)> node)
    {
        Debug.Assert(node.List == null, "Node is already in the list");
        _lruList.AddLast(node);
        _dict.Add(node.Value.Item1, node);
    }

    private void TrimToCapacityNoLock()
    {
        if (_lruList.Count > _capacity)
        {
            var node = _lruList.First;
            if (node != null)
            {
                _lruList.RemoveFirst();
                _dict.Remove(node.Value.Item1);
            }
        }
    }
}
