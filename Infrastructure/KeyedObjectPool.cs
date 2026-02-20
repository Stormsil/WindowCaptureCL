using System.Collections.Concurrent;

namespace WindowCaptureCL.Infrastructure;

/// <summary>
/// Keyed object pool that maintains separate pools for different key values.
/// Useful for pooling objects that vary by size or other parameters.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of objects to pool.</typeparam>
public sealed class KeyedObjectPool<TKey, TValue> where TKey : notnull where TValue : class
{
    private readonly ConcurrentDictionary<TKey, ObjectPool<TValue>> _pools;
    private readonly Func<TKey, TValue> _factory;
    private readonly Action<TValue>? _resetAction;
    private readonly Action<TValue>? _disposeAction;
    private readonly int _maxSizePerKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedObjectPool{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="factory">Factory function to create new objects. Receives the key as parameter.</param>
    /// <param name="resetAction">Optional action to reset object state before returning to pool.</param>
    /// <param name="disposeAction">Optional action to dispose objects when pool is cleared.</param>
    /// <param name="maxSizePerKey">Maximum number of objects per key. Default is 5.</param>
    public KeyedObjectPool(
        Func<TKey, TValue> factory,
        Action<TValue>? resetAction = null,
        Action<TValue>? disposeAction = null,
        int maxSizePerKey = 5)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _resetAction = resetAction;
        _disposeAction = disposeAction;
        _maxSizePerKey = maxSizePerKey > 0 ? maxSizePerKey : 5;
        _pools = new ConcurrentDictionary<TKey, ObjectPool<TValue>>();
    }

    /// <summary>
    /// Gets an object from the pool for the specified key.
    /// </summary>
    /// <param name="key">The key to identify the pool.</param>
    /// <returns>An object of type TValue.</returns>
    public TValue Get(TKey key)
    {
        var pool = _pools.GetOrAdd(key, k => new ObjectPool<TValue>(
            () => _factory(k),
            _resetAction,
            _disposeAction,
            _maxSizePerKey));

        return pool.Get();
    }

    /// <summary>
    /// Returns an object to the pool for the specified key.
    /// </summary>
    /// <param name="key">The key to identify the pool.</param>
    /// <param name="value">The object to return.</param>
    public void Return(TKey key, TValue value)
    {
        if (_pools.TryGetValue(key, out var pool))
        {
            pool.Return(value);
        }
        else
        {
            // If pool doesn't exist, dispose the object.
            _disposeAction?.Invoke(value);
        }
    }

    /// <summary>
    /// Clears all pools and disposes all objects.
    /// </summary>
    public void Clear()
    {
        foreach (var pool in _pools.Values)
        {
            pool.Clear();
        }

        _pools.Clear();
    }

    /// <summary>
    /// Clears the pool for a specific key.
    /// </summary>
    /// <param name="key">The key of the pool to clear.</param>
    public void Clear(TKey key)
    {
        if (_pools.TryRemove(key, out var pool))
        {
            pool.Clear();
        }
    }
}
