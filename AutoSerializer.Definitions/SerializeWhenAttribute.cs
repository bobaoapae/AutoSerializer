using System;

namespace AutoSerializer.Definitions
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SerializeWhenAttribute : Attribute
    {
        public string Value { get; }

        public SerializeWhenAttribute(string value)
        {
            Value = value;
        }
    }
}
