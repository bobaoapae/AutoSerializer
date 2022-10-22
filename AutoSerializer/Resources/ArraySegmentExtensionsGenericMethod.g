public static void Read(this ArraySegment<byte> buffer, ref int offset, out {0} value)
{{
    value = new {0}();
    value.Deserialize(buffer, ref offset);
}}


public static void Read(this ArraySegment<byte> buffer, ref int offset, in int size, out List<{0}> value)
{{
    value = new List<{0}>(size);
    for (var i = 0; i < size; i++)
    {{
        buffer.Read(ref offset, out {0} val);
        value.Add(val);
    }}
}}

public static void Read<T>(this ArraySegment<byte> buffer, ref int offset, in int size, out {0}[] value)
{{
    value = new {0}[size];
    for (var i = 0; i < size; i++)
    {{
        buffer.Read(ref offset, out {0} val);
        value[i] = val;
    }}
}}