using System;

namespace AutoSerializer.Definitions
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FieldLengthAttribute : Attribute
    {
        public string Value { get; }

        public FieldLengthAttribute(int value)
        {
            Value = value.ToString();
        }

        public FieldLengthAttribute(string value)
        {
            Value = value;
        }
    }
}
