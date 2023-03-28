using System;

namespace AutoSerializer.Definitions
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FieldCountAttribute : Attribute
    {
        public string CurCount { get; }
        public int MaxCount { get; }

        public FieldCountAttribute(string curCount, int maxCount)
        {
            CurCount = curCount;
            MaxCount = maxCount;
        }

        public FieldCountAttribute(int curCount, int maxCount)
        {
            CurCount = curCount.ToString();
            MaxCount = maxCount;
        }

        public FieldCountAttribute(string curCount)
        {
            CurCount = curCount;
            MaxCount = 0;
        }
    }
}