namespace AutoSerializer.Definitions;

public static unsafe class DeserializeUtils
{
    public static void Read(ref byte* bytePtr, out int* value)
    {
        value = (int*)bytePtr;
        bytePtr += sizeof(int);
    }

    public static void Read(ref byte* bytePtr, out uint* value)
    {
        value = (uint*)bytePtr;
        bytePtr += sizeof(uint);
    }

    public static void Read(ref byte* bytePtr, out long* value)
    {
        value = (long*)bytePtr;
        bytePtr += sizeof(long);
    }

    public static void Read(ref byte* bytePtr, out ulong* value)
    {
        value = (ulong*)bytePtr;
        bytePtr += sizeof(ulong);
    }

    public static void Read(ref byte* bytePtr, out short* value)
    {
        value = (short*)bytePtr;
        bytePtr += sizeof(short);
    }

    public static void Read(ref byte* bytePtr, out ushort* value)
    {
        value = (ushort*)bytePtr;
        bytePtr += sizeof(ushort);
    }

    public static void Read(ref byte* bytePtr, out byte* value)
    {
        value = (byte*)bytePtr;
        bytePtr += sizeof(byte);
    }

    public static void Read(ref byte* bytePtr, out sbyte* value)
    {
        value = (sbyte*)bytePtr;
        bytePtr += sizeof(sbyte);
    }

    public static void Read(ref byte* bytePtr, out float* value)
    {
        value = (float*)bytePtr;
        bytePtr += sizeof(float);
    }

    public static void Read(ref byte* bytePtr, out double* value)
    {
        value = (double*)bytePtr;
        bytePtr += sizeof(double);
    }

    public static void Read(ref byte* bytePtr, out bool* value)
    {
        value = (bool*)bytePtr;
        bytePtr += sizeof(bool);
    }

    public static void Read(ref byte* bytePtr, out char* value)
    {
        value = (char*)bytePtr;
        bytePtr += sizeof(char);
    }
}