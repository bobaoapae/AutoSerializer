using System;

namespace AutoSerializer.Definitions
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FieldCountAttribute : Attribute
    {
        public string Value { get; }

        public FieldCountAttribute(int value)
        {
            Value = value.ToString();
        }

        public FieldCountAttribute(string value)
        {
            Value = value;
        }
    }
}
