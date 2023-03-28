using System;

namespace AutoSerializer.Definitions
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeWhenAttribute : Attribute
    {
        public string Value { get; }

        public SerializeWhenAttribute(string value)
        {
            Value = value;
        }
    }
}
