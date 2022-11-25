using System;
using Newtonsoft.Json.Linq;

namespace AutoSerializer.Definitions
{
    public interface IAutoDeserialize
    {
        void Deserialize(in ArraySegment<byte> buffer, ref int offset);
    }
}
