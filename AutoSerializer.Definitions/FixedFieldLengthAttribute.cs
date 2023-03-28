using System;

namespace AutoSerializer.Definitions
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FixedFieldLengthAttribute : Attribute
    {
        public string Value { get; }

        public FixedFieldLengthAttribute(int value)
        {
            Value = value.ToString();
        }

        public FixedFieldLengthAttribute(string value)
        {
            Value = value;
        }
    }
}
