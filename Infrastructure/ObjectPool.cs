using System.Collections.Concurrent;

namespace WindowCaptureCL.Infrastructure;

/// <summary>
/// Generic object pool for efficient reuse of expensive objects.
/// Thread-safe implementation using ConcurrentBag for storage.
/// </summary>
/// <typeparam name="T">The type of objects to pool. Must be a class.</typeparam>
public sealed class ObjectPool<T> where T : class
{
    private readonly ConcurrentBag<T> _pool;
    private readonly Func<T> _factory;
    private readonly Action<T>? _resetAction;
    private readonly Action<T>? _disposeAction;
    private readonly int _maxSize;
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.
    /// </summary>
    /// <param name="factory">Factory function to create new objects when pool is empty.</param>
    /// <param name="resetAction">Optional action to reset object state before returning to pool.</param>
    /// <param name="disposeAction">Optional action to dispose objects when pool is cleared.</param>
    /// <param name="maxSize">Maximum number of objects to keep in the pool. Default is 10.</param>
    /// <exception cref="ArgumentNullException">Thrown when factory is null.</exception>
    public ObjectPool(
        Func<T> factory,
        Action<T>? resetAction = null,
        Action<T>? disposeAction = null,
        int maxSize = 10)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _resetAction = resetAction;
        _disposeAction = disposeAction;
        _maxSize = maxSize > 0 ? maxSize : 10;
        _pool = new ConcurrentBag<T>();
        _count = 0;
    }

    /// <summary>
    /// Gets an object from the pool or creates a new one if pool is empty.
    /// </summary>
    /// <returns>An object of type T.</returns>
    public T Get()
    {
        if (_pool.TryTake(out var obj))
        {
            Interlocked.Decrement(ref _count);
            return obj;
        }

        return _factory();
    }

    /// <summary>
    /// Returns an object to the pool for reuse.
    /// If pool is at max capacity, the object is disposed.
    /// </summary>
    /// <param name="obj">The object to return to the pool.</param>
    public void Return(T obj)
    {
        if (obj == null)
            return;

        // Reset object state if reset action provided
        _resetAction?.Invoke(obj);

        // Only add to pool if under max size
        if (Interlocked.Increment(ref _count) <= _maxSize)
        {
            _pool.Add(obj);
        }
        else
        {
            Interlocked.Decrement(ref _count);
            _disposeAction?.Invoke(obj);
        }
    }

    /// <summary>
    /// Clears the pool and disposes all pooled objects.
    /// </summary>
    public void Clear()
    {
        while (_pool.TryTake(out var obj))
        {
            Interlocked.Decrement(ref _count);
            _disposeAction?.Invoke(obj);
        }
    }

    /// <summary>
    /// Gets the current number of objects in the pool.
    /// </summary>
    public int Count => _count;
}
