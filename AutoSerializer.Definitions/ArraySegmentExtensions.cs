using System;
using System.Collections.Generic;
using System.Text;

namespace AutoSerializer.Definitions
{
    public static class ArraySegmentExtensions
    {
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
            buffer.Read(ref offset, out long seconds);
            dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(seconds);
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