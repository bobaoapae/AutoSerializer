using System;

namespace AutoSerializer.Definitions
{
    public class AutoDeserializeAttribute : Attribute
    {
        public bool IsDynamic { get; set; }
    }
}
