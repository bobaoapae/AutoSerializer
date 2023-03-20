using System;

namespace AutoSerializer.Definitions;

public interface IAutoPooled<out T> : IDisposable where T : new()
{
    public static abstract T Create();
    public void Initialize();
    public void CleanObject();
}