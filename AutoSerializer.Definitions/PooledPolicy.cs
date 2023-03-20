using Microsoft.Extensions.ObjectPool;

namespace AutoSerializer.Definitions;

public class PooledPolicy<T> : IPooledObjectPolicy<T> where T : IAutoPooled<T>, new()
{
    public T Create()
    {
        var obj = new T();
        obj.Initialize();
        return obj;
    }

    public bool Return(T obj)
    {
        obj.CleanObject();
        return true;
    }
}