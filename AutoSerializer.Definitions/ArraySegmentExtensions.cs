using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace AutoSerializer.Definitions
{
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    public static class ArraySegmentExtensions
    {
        public static void Read(this ArraySegment<byte> buffer, ref int offset, out bool value)
        {
            value = buffer.Array[offset++] == 1;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out byte value)
        {
            value = buffer.Array[offset++];
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out sbyte value)
        {
            value = (sbyte)buffer.Array[offset++];
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out short value)
        {
            value = BitConverter.ToInt16(buffer.Array, offset);
            offset += 2;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out ushort value)
        {
            value = BitConverter.ToUInt16(buffer.Array, offset);
            offset += 2;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out int value)
        {
            value = BitConverter.ToInt32(buffer.Array, offset);
            offset += 4;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out uint value)
        {
            value = BitConverter.ToUInt32(buffer.Array, offset);
            offset += 4;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out float value)
        {
            value = BitConverter.ToSingle(buffer.Array, offset);
            offset += 4;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out long value)
        {
            value = BitConverter.ToInt64(buffer.Array, offset);
            offset += 8;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out ulong value)
        {
            value = BitConverter.ToUInt64(buffer.Array, offset);
            offset += 8;
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out string value)
        {
            value = Encoding.UTF8.GetString(buffer.Array, offset, size);
            offset += size;
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
                value[i] = BitConverter.ToInt32(buffer.Array, offset);
                offset += 4;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out uint[] value)
        {
            value = new uint[size];
            for (var i = 0; i < size; i++)
            {
                value[i] = BitConverter.ToUInt32(buffer.Array, offset);
                offset += 4;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out long[] value)
        {
            value = new long[size];
            for (var i = 0; i < size; i++)
            {
                value[i] = BitConverter.ToInt64(buffer.Array, offset);
                offset += 8;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out ulong[] value)
        {
            value = new ulong[size];
            for (var i = 0; i < size; i++)
            {
                value[i] = BitConverter.ToUInt64(buffer.Array, offset);
                offset += 8;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out float[] value)
        {
            value = new float[size];
            for (var i = 0; i < size; i++)
            {
                value[i] = BitConverter.ToSingle(buffer.Array, offset);
                offset += 4;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out short[] value)
        {
            value = new short[size];
            for (var i = 0; i < size; i++)
            {
                value[i] = BitConverter.ToInt16(buffer.Array, offset);
                offset += 2;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out ushort[] value)
        {
            value = new ushort[size];
            for (var i = 0; i < size; i++)
            {
                value[i] = BitConverter.ToUInt16(buffer.Array, offset);
                offset += 2;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out Guid guid)
        {
            buffer.Read(ref offset, out int guidLen);
            buffer.Read(ref offset, guidLen, out byte[] bytes);
            guid = new Guid(bytes);
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out string value)
        {
            buffer.Read(ref offset, out int strLen);
            buffer.Read(ref offset, strLen, out value);
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out byte[] value)
        {
            buffer.Read(ref offset, out int arrayLen);
            buffer.Read(ref offset, arrayLen, out value);
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out sbyte[] value)
        {
            buffer.Read(ref offset, out int arrayLen);
            buffer.Read(ref offset, arrayLen, out value);
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out int[] value)
        {
            buffer.Read(ref offset, out int arrayLen);
            buffer.Read(ref offset, arrayLen, out value);
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out uint[] value)
        {
            buffer.Read(ref offset, out int arrayLen);
            buffer.Read(ref offset, arrayLen, out value);
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out short[] value)
        {
            buffer.Read(ref offset, out int arrayLen);
            buffer.Read(ref offset, arrayLen, out value);
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out ushort[] value)
        {
            buffer.Read(ref offset, out int arrayLen);
            buffer.Read(ref offset, arrayLen, out value);
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out long[] value)
        {
            buffer.Read(ref offset, out int arrayLen);
            buffer.Read(ref offset, arrayLen, out value);
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out ulong[] value)
        {
            buffer.Read(ref offset, out int arrayLen);
            buffer.Read(ref offset, arrayLen, out value);
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out float[] value)
        {
            buffer.Read(ref offset, out int arrayLen);
            buffer.Read(ref offset, arrayLen, out value);
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out string[] value)
        {
            buffer.Read(ref offset, out int arrayLen);
            value = new string[arrayLen];
            for (int i = 0; i < arrayLen; i++)
            {
                buffer.Read(ref offset, out string val);
                value[i] = val;
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out List<byte> value)
        {
            buffer.Read(ref offset, out int arrayLen);
            value = new List<byte>(arrayLen);
            for (int i = 0; i < arrayLen; i++)
            {
                buffer.Read(ref offset, out byte val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out List<sbyte> value)
        {
            buffer.Read(ref offset, out int arrayLen);
            value = new List<sbyte>(arrayLen);
            for (int i = 0; i < arrayLen; i++)
            {
                buffer.Read(ref offset, out sbyte val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out List<int> value)
        {
            buffer.Read(ref offset, out int arrayLen);
            value = new List<int>(arrayLen);
            for (int i = 0; i < arrayLen; i++)
            {
                buffer.Read(ref offset, out int val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out List<uint> value)
        {
            buffer.Read(ref offset, out int arrayLen);
            value = new List<uint>(arrayLen);
            for (int i = 0; i < arrayLen; i++)
            {
                buffer.Read(ref offset, out uint val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out List<short> value)
        {
            buffer.Read(ref offset, out int arrayLen);
            value = new List<short>(arrayLen);
            for (int i = 0; i < arrayLen; i++)
            {
                buffer.Read(ref offset, out short val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out List<ushort> value)
        {
            buffer.Read(ref offset, out int arrayLen);
            value = new List<ushort>(arrayLen);
            for (int i = 0; i < arrayLen; i++)
            {
                buffer.Read(ref offset, out ushort val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out List<long> value)
        {
            buffer.Read(ref offset, out int arrayLen);
            value = new List<long>(arrayLen);
            for (int i = 0; i < arrayLen; i++)
            {
                buffer.Read(ref offset, out long val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out List<ulong> value)
        {
            buffer.Read(ref offset, out int arrayLen);
            value = new List<ulong>(arrayLen);
            for (int i = 0; i < arrayLen; i++)
            {
                buffer.Read(ref offset, out ulong val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out List<float> value)
        {
            buffer.Read(ref offset, out int arrayLen);
            value = new List<float>(arrayLen);
            for (int i = 0; i < arrayLen; i++)
            {
                buffer.Read(ref offset, out float val);
                value.Add(val);
            }
        }

        public static void Read(this ArraySegment<byte> buffer, ref int offset, out List<string> value)
        {
            buffer.Read(ref offset, out int arrayLen);
            value = new List<string>(arrayLen);
            for (int i = 0; i < arrayLen; i++)
            {
                buffer.Read(ref offset, out string val);
                value.Add(val);
            }
        }
    }
}