using System.IO;

namespace AutoSerializer.Definitions
{
    public interface IAutoSerialize
    {
        void Serialize(MemoryStream stream);
    }
}
