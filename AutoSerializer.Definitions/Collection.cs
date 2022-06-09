using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.IO;

namespace AutoSerializer.Definitions
{
    public class Collection<T> : List<T> where T : IAutoDeserialize, IAutoSerialize, new()
    {
        private static readonly RecyclableMemoryStreamManager manager;

        static Collection()
        {
            int blockSize = 1024;
            int largeBufferMultiple = 1024 * 1024;
            int maxBufferSize = 16 * largeBufferMultiple;

            manager = new RecyclableMemoryStreamManager(blockSize, largeBufferMultiple, maxBufferSize)
            {
                AggressiveBufferReturn = true,
                MaximumFreeSmallPoolBytes = blockSize * 2048,
                MaximumFreeLargePoolBytes = maxBufferSize * 4
            };
        }

        public Collection(IEnumerable<T> items)
        {
            AddRange(items);
        }

        public Collection(int capacity = 0) : base(capacity)
        {
        }

        public byte[] Serialize()
        {
            const int minimumSize = 1024;

            var stream = (RecyclableMemoryStream)manager.GetStream($"Collection.Serialize<{typeof(T)}>", minimumSize, true);

            try
            {
                Serialize(stream);

                var buffer = new byte[stream.Length];
                Array.ConstrainedCopy(stream.GetBuffer(), 0, buffer, 0, buffer.Length);

                return buffer;
            }
            finally
            {
                stream.Dispose();
            }
        }

        public void Serialize(MemoryStream memoryStream)
        {
            memoryStream.ExWrite(BitConverter.GetBytes(Count));
            for (var i = 0; i < Count; i++)
                this[i].Serialize(memoryStream);
        }

        public static Collection<T> Deserialize(byte[] data)
        {
            if (data?.Length < sizeof(int))
                return new Collection<T>();

            var itemCount = BitConverter.ToInt32(data!, 0);
            var collection = new Collection<T>(itemCount);

            var offset = sizeof(int);
            for (var i = 0; i < itemCount; i++)
            {
                var instance = new T();
                instance.Deserialize(new ArraySegment<byte>(data), ref offset);
                collection.Add(instance);
            }

            return collection;
        }
    }
}
