using System;

namespace AutoSerializer.Definitions
{
    public unsafe interface IAutoDeserialize<out T> where T : struct, IAutoDeserialize<T>
    {
        static abstract T Deserialize(ref byte* bytePtr);
        static abstract T Deserialize(ArraySegment<byte> data);
        static abstract T Deserialize(Span<byte> data);
    }
}