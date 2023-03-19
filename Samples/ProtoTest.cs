using AutoSerializer.Definitions;
using Collections.Pooled;

namespace Samples;

[AutoSerialize]
public partial class ProtoTest
{
    public uint U0 { get; set; }
    public byte U1_Count { get; set; }
    [FieldCount(nameof(U1_Count))] public PooledList<uint> U1 { get; set; }
    public byte U2_Count { get; set; }
    [FieldCount(nameof(U2_Count))] public PooledList<string> U2 { get; set; }
    public EnumTest U3 { get; set; }
    public byte U4_Count { get; set; }
    [FieldCount(nameof(U4_Count))] public PooledList<InternalProtoTest> U4 { get; set; }
}

[AutoSerialize]
public partial class ExtendedProtoTest : ProtoTest
{
    public byte U5_Count { get; set; }
    [FieldCount(nameof(U5_Count))] public PooledList<string> U5 { get; set; }
}

[AutoSerialize]
public partial class InternalProtoTest
{
}

public enum EnumTest
{
    VALUE,
    VALUE2
}

[AutoDeserialize]
public partial class DeserializeProtoTest
{
    public uint U0 { get; set; }
    public byte U1_Count { get; set; }
    [FieldCount(nameof(U1_Count))] public PooledList<uint> U1 { get; set; }
    public byte U2_Count { get; set; }
    [FieldCount(nameof(U2_Count))] public PooledList<string> U2 { get; set; }
    public EnumTest U3 { get; set; }
    public byte U4_Count { get; set; }
    [FieldCount(nameof(U4_Count))] public PooledList<InternalDeserializeProtoTest> U4 { get; set; }
}

[AutoDeserialize]
public partial class ExtendedDeserializeProtoTest : DeserializeProtoTest
{
    public byte U5_Count { get; set; }
    [FieldCount(nameof(U5_Count))] public PooledList<string> U5 { get; set; }
}

[AutoDeserialize]
public partial class InternalDeserializeProtoTest
{
}

[AutoDeserialize]
[AutoSerialize]
public partial class SerializeDeserializeProtoTest
{
    public uint U0 { get; set; }
    public byte U1_Count { get; set; }
    [FieldCount(nameof(U1_Count))] public PooledList<uint> U1 { get; set; }
    public byte U2_Count { get; set; }
    [FieldCount(nameof(U2_Count))] public PooledList<string> U2 { get; set; }
    public EnumTest U3 { get; set; }
    public byte U4_Count { get; set; }
    [FieldCount(nameof(U4_Count))] public PooledList<InternalSerializeDeserializeProtoTest> U4 { get; set; }
}

[AutoDeserialize]
[AutoSerialize]
public partial class ExtendedSerializeDeserializeProtoTest : SerializeDeserializeProtoTest
{
    public byte U5_Count { get; set; }
    [FieldCount(nameof(U5_Count))] public PooledList<string> U5 { get; set; }
}

[AutoDeserialize]
[AutoSerialize]
public partial class InternalSerializeDeserializeProtoTest
{
}