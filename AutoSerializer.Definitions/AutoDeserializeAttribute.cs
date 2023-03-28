using System;

namespace AutoSerializer.Definitions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class AutoDeserializeAttribute : Attribute
    {
        public bool IsDynamic { get; set; }
    }
}
