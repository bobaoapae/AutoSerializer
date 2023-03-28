using AutoSerializer.Definitions;

[AutoDeserialize]
public unsafe partial struct SampleStruct
{
    private int* FirstValue;
    private int* SecondValue;
    [FieldCount(nameof(SecondValue))] private byte* StringValue;
    private long* FourValue;
    private OtherStruct OtherStruct;
    private uint* FiveValue;
    [FieldCount(nameof(FiveValue), 50)] private OtherStruct OtherStructs;
    private bool* HasValueSix;
    [SerializeWhen(nameof(HasValueSix))] private uint* ValueSix;
    [FixedFieldLength(4)] private bool* HasValueSeven;
}

[AutoDeserialize]
public unsafe partial struct OtherStruct
{
}