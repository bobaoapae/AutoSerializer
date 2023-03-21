using System;
using Collections.Pooled;

namespace AutoSerializer.Definitions;

public interface IAutoPooled<T> : IDisposable where T : new()
{
    public static abstract T Create();
    public static abstract PooledList<T> CreateList();
    public void Initialize();
    public void CleanObject();
}