using System;
using System.Collections.Generic;
using System.Text;
using Collections.Pooled;
using Microsoft.Extensions.ObjectPool;

namespace AutoSerializer.Definitions
{
    public static class ArraySegmentExtensions
    {
        private static ObjectPool<PooledList<byte>> _listBytePool = new DefaultObjectPool<PooledList<byte>>(new DefaultPooledObjectPolicy<PooledList<byte>>());
        private static ObjectPool<PooledList<sbyte>> _listSBytePool = new DefaultObjectPool<PooledList<sbyte>>(new DefaultPooledObjectPolicy<PooledList<sbyte>>());
        private static ObjectPool<PooledList<short>> _listShortPool = new DefaultObjectPool<PooledList<short>>(new DefaultPooledObjectPolicy<PooledList<short>>());
        private static ObjectPool<PooledList<ushort>> _listUShortPool = new DefaultObjectPool<PooledList<ushort>>(new DefaultPooledObjectPolicy<PooledList<ushort>>());
        private static ObjectPool<PooledList<int>> _listIntPool = new DefaultObjectPool<PooledList<int>>(new DefaultPooledObjectPolicy<PooledList<int>>());
        private static ObjectPool<PooledList<uint>> _listUIntPool = new DefaultObjectPool<PooledList<uint>>(new DefaultPooledObjectPolicy<PooledList<uint>>());
        private static ObjectPool<PooledList<long>> _listLongPool = new DefaultObjectPool<PooledList<long>>(new DefaultPooledObjectPolicy<PooledList<long>>());
        private static ObjectPool<PooledList<ulong>> _listULongPool = new DefaultObjectPool<PooledList<ulong>>(new DefaultPooledObjectPolicy<PooledList<ulong>>());
        private static ObjectPool<PooledList<float>> _listFloatPool = new DefaultObjectPool<PooledList<float>>(new DefaultPooledObjectPolicy<PooledList<float>>());
        private static ObjectPool<PooledList<string>> _listStringPool = new DefaultObjectPool<PooledList<string>>(new DefaultPooledObjectPolicy<PooledList<string>>());

        public static void ReturnList(PooledList<byte> list)
        {
            list.Clear();
            list.Dispose();
            _listBytePool.Return(list);
        }

        public static void ReturnList(PooledList<sbyte> list)
        {
            list.Clear();
            list.Dispose();
            _listSBytePool.Return(list);
        }

        public static void ReturnList(PooledList<short> list)
        {
            list.Clear();
            list.Dispose();
            _listShortPool.Return(list);
        }

        public static void ReturnList(PooledList<ushort> list)
        {
            list.Clear();
            list.Dispose();
            _listUShortPool.Return(list);
        }

        public static void ReturnList(PooledList<int> list)
        {
            list.Clear();
            list.Dispose();
            _listIntPool.Return(list);
        }

        public static void ReturnList(PooledList<uint> list)
        {
            list.Clear();
            list.Dispose();
            _listUIntPool.Return(list);
        }

        public static void ReturnList(PooledList<long> list)
        {
            list.Clear();
            list.Dispose();
            _listLongPool.Return(list);
        }

        public static void ReturnList(PooledList<ulong> list)
        {
            list.Clear();
            list.Dispose();
            _listULongPool.Return(list);
        }
        
        public static void ReturnList(PooledList<float> list)
        {
            list.Clear();
            list.Dispose();
            _listFloatPool.Return(list);
        }

        public static void ReturnList(PooledList<string> list)
        {
            list.Clear();
            list.Dispose();
            _listStringPool.Return(list);
        }
        
        public static PooledList<byte> GetListByte()
        {
            return _listBytePool.Get();
        }
        
        public static PooledList<sbyte> GetListSbyte()
        {
            return _listSBytePool.Get();
        }
        
        public static PooledList<short> GetListShort()
        {
            return _listShortPool.Get();
        }
        
        public static PooledList<ushort> GetListUshort()
        {
            return _listUShortPool.Get();
        }
        
        public static PooledList<int> GetListInt()
        {
            return _listIntPool.Get();
        }
        
        public static PooledList<uint> GetListUint()
        {
            return _listUIntPool.Get();
        }
        
        public static PooledList<long> GetListLong()
        {
            return _listLongPool.Get();
        }
        
        public static PooledList<ulong> GetListUlong()
        {
            return _listULongPool.Get();
        }
        
        public static PooledList<float> GetListFloat()
        {
            return _listFloatPool.Get();
        }
        
        public static PooledList<string> GetListString()
        {
            return _listStringPool.Get();
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out bool value)
        {
            value = buffer.Array![offset++] == 1;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out byte value)
        {
            value = buffer.Array![offset++];
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out sbyte value)
        {
            value = (sbyte)buffer.Array![offset++];
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out short value)
        {
            value = BitConverter.ToInt16(buffer.Array!, offset);
            offset += 2;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out ushort value)
        {
            value = BitConverter.ToUInt16(buffer.Array!, offset);
            offset += 2;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out int value)
        {
            value = BitConverter.ToInt32(buffer.Array!, offset);
            offset += 4;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out uint value)
        {
            value = BitConverter.ToUInt32(buffer.Array!, offset);
            offset += 4;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out float value)
        {
            value = BitConverter.ToSingle(buffer.Array!, offset);
            offset += 4;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out long value)
        {
            value = BitConverter.ToInt64(buffer.Array!, offset);
            offset += 8;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out ulong value)
        {
            value = BitConverter.ToUInt64(buffer.Array!, offset);
            offset += 8;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out DateTimeOffset dateTimeOffset)
        {
            buffer.Read(ref offset, out int offsetHours);
            buffer.Read(ref offset, out long seconds);
            dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(seconds).ToOffset(TimeSpan.FromHours(offsetHours));
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out DateTime dateTime)
        {
            buffer.Read(ref offset, out DateTimeOffset dateTimeOffset);
            dateTime = dateTimeOffset.DateTime;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out string value)
        {
            value = Encoding.UTF8.GetString(buffer.Array!, offset, size);
            offset += size;
        }

        public static void Read<T>(this ArraySegment<byte> buffer, ref int offset, out T value) where T : IAutoDeserialize, new()
        {
            value = new T();
            value.Deserialize(buffer, ref offset);
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out byte[] value)
        {
            value = new byte[size];
            Array.ConstrainedCopy(buffer.Array, offset, value, 0, size);
            offset += size;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out sbyte[] value)
        {
            value = new sbyte[size];
            Array.ConstrainedCopy(buffer.Array, offset, value, 0, size);
            offset += size;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out int[] value)
        {
            value = new int[size];
            for (var i = 0; i < size; i++)
            {
                value[i] = BitConverter.ToInt32(buffer.Array!, offset);
                offset += 4;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out uint[] value)
        {
            value = new uint[size];
            for (var i = 0; i < size; i++)
            {
                value[i] = BitConverter.ToUInt32(buffer.Array!, offset);
                offset += 4;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out long[] value)
        {
            value = new long[size];
            for (var i = 0; i < size; i++)
            {
                value[i] = BitConverter.ToInt64(buffer.Array!, offset);
                offset += 8;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out ulong[] value)
        {
            value = new ulong[size];
            for (var i = 0; i < size; i++)
            {
                value[i] = BitConverter.ToUInt64(buffer.Array!, offset);
                offset += 8;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out float[] value)
        {
            value = new float[size];
            for (var i = 0; i < size; i++)
            {
                value[i] = BitConverter.ToSingle(buffer.Array!, offset);
                offset += 4;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out short[] value)
        {
            value = new short[size];
            for (var i = 0; i < size; i++)
            {
                value[i] = BitConverter.ToInt16(buffer.Array!, offset);
                offset += 2;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out ushort[] value)
        {
            value = new ushort[size];
            for (var i = 0; i < size; i++)
            {
                value[i] = BitConverter.ToUInt16(buffer.Array!, offset);
                offset += 2;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out string[] value)
        {
            value = new string[size];
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out int strLen);
                buffer.Read(ref offset, strLen, out string val);
                value[i] = val;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out Guid guid)
        {
            buffer.Read(ref offset, 16, out byte[] bytes);
            guid = new Guid(bytes);
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out List<byte> value)
        {
            value = new List<byte>(size);
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out byte val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out List<sbyte> value)
        {
            value = new List<sbyte>(size);
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out sbyte val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out List<int> value)
        {
            value = new List<int>(size);
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out int val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out List<uint> value)
        {
            value = new List<uint>(size);
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out uint val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out List<short> value)
        {
            value = new List<short>(size);
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out short val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out List<ushort> value)
        {
            value = new List<ushort>(size);
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out ushort val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out List<long> value)
        {
            value = new List<long>(size);
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out long val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out List<ulong> value)
        {
            value = new List<ulong>(size);
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out ulong val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out List<float> value)
        {
            value = new List<float>(size);
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out float val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out List<string> value)
        {
            value = new List<string>(size);
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out int strLen);
                buffer.Read(ref offset, strLen, out string val);
                value.Add(val);
            }
        }

        public static void Read<T>(this ArraySegment<byte> buffer, ref int offset, in int size, out List<T> value) where T : IAutoDeserialize, new()
        {
            value = new List<T>(size);
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out T val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out Collections.Pooled.PooledList<byte> value)
        {
            value = _listBytePool.Get();
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out byte val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out Collections.Pooled.PooledList<sbyte> value)
        {
            value = _listSBytePool.Get();
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out sbyte val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out Collections.Pooled.PooledList<int> value)
        {
            value = _listIntPool.Get();
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out int val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out Collections.Pooled.PooledList<uint> value)
        {
            value = _listUIntPool.Get();
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out uint val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out Collections.Pooled.PooledList<short> value)
        {
            value = _listShortPool.Get();
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out short val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out Collections.Pooled.PooledList<ushort> value)
        {
            value = _listUShortPool.Get();
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out ushort val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out Collections.Pooled.PooledList<long> value)
        {
            value = _listLongPool.Get();
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out long val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out Collections.Pooled.PooledList<ulong> value)
        {
            value = _listULongPool.Get();
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out ulong val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out Collections.Pooled.PooledList<float> value)
        {
            value = _listFloatPool.Get();
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out float val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out Collections.Pooled.PooledList<string> value)
        {
            value = _listStringPool.Get();
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out int strLen);
                buffer.Read(ref offset, strLen, out string val);
                value.Add(val);
            }
        }

        public static void Read<T>(this ArraySegment<byte> buffer, ref int offset, in int size, out T[] value) where T : IAutoDeserialize, new()
        {
            value = new T[size];
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out T val);
                value[i] = val;
            }
        }

        public static void Read<T>(this ArraySegment<byte> buffer, ref int offset, in int size, out Collection<T> value) where T : IAutoDeserialize, IAutoSerialize, new()
        {
            value = new Collection<T>(size);
            for (var i = 0; i < size; i++)
            {
                buffer.Read(ref offset, out T val);
                value.Add(val);
            }
        }
    }
}