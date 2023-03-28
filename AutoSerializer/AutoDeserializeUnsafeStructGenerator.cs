using System;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AutoSerializer;

public class AutoDeserializeUnsafeStructGenerator
{
    public static void Generate(Compilation compilation, ImmutableArray<INamedTypeSymbol> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return;
        }

        try
        {
            var attributeSymbol = compilation.GetTypeByMetadataName("AutoSerializer.Definitions.AutoDeserializeAttribute");
            var fieldCountAttributeSymbol = compilation.GetTypeByMetadataName("AutoSerializer.Definitions.FieldCountAttribute");
            var serializeWhenAttributeSymbol = compilation.GetTypeByMetadataName("AutoSerializer.Definitions.SerializeWhenAttribute");
            var fixedFieldLengthAttributeSymbol = compilation.GetTypeByMetadataName("AutoSerializer.Definitions.FixedFieldLengthAttribute");

            foreach (var structSymbol in classes)
            {
                var privateSupportedFields = structSymbol.GetMembers().OfType<IFieldSymbol>().Where(x => x.IsStatic == false && x.IsReadOnly == false && (x.Type is IPointerTypeSymbol || x.Type.TypeKind == TypeKind.Struct))
                    .ToList();
                var privateSupportedFieldsNeedMaxCount = privateSupportedFields
                    .Where(x => x.Type.TypeKind == TypeKind.Struct && x.GetAttributes().Any(y => y.AttributeClass!.Equals(fieldCountAttributeSymbol, SymbolEqualityComparer.Default)))
                    .ToList();

                var hasInvalidField = false;
                foreach (var fieldSymbol in privateSupportedFieldsNeedMaxCount)
                {
                    var attribute = fieldSymbol.GetAttributes().First(x => x.AttributeClass!.Equals(fieldCountAttributeSymbol, SymbolEqualityComparer.Default));
                    //check if fieldCountAttribute has a fieldCount
                    var fieldMaxCount = attribute.ConstructorArguments.Length == 2 ? (int)attribute.ConstructorArguments[1].Value! : 0;
                    if (fieldMaxCount != 0)
                        continue;
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "AS0002",
                                "AutoSerializer",
                                $"FieldCountAttribute on {fieldSymbol.Name} does not have a maxCount",
                                "AutoSerializer",
                                DiagnosticSeverity.Error,
                                true),
                            fieldSymbol.Locations.First()
                        )
                    );
                    hasInvalidField = true;
                }

                foreach (var privateSupportedField in privateSupportedFields)
                {
                    if (privateSupportedField.Type.TypeKind == TypeKind.Struct)
                    {
                        if (!privateSupportedField.Type.Interfaces.Any(symbol => symbol.Name == "AutoDeserialize") &
                            privateSupportedField.Type.OriginalDefinition.GetAttributes().All(data => !data.AttributeClass!.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    new DiagnosticDescriptor(
                                        "AS0002",
                                        "AutoSerializer",
                                        $"{privateSupportedField.Name} it's not supported, target class should implement AutoDeserialize",
                                        "AutoSerializer",
                                        DiagnosticSeverity.Error,
                                        true),
                                    privateSupportedField.Locations.First()
                                )
                            );
                            hasInvalidField = true;
                        }
                    }
                }

                if (hasInvalidField)
                    continue;

                var indentedWriter = new IndentedTextWriter(new StringWriter(), "    ");
                indentedWriter.WriteLine("using System;");
                indentedWriter.WriteLine("using System.IO;");
                indentedWriter.WriteLine("using AutoSerializer.Definitions;");
                indentedWriter.WriteLine("using System.Runtime.CompilerServices;");
                indentedWriter.WriteLine("using  System.Runtime.InteropServices;");
                indentedWriter.WriteLine();
                if (structSymbol.ContainingNamespace.ToDisplayString() != "<global namespace>")
                    indentedWriter.WriteLine($"namespace {structSymbol.ContainingNamespace.ToDisplayString()};");

                indentedWriter.WriteLine("[StructLayout(LayoutKind.Auto)]");
                indentedWriter.Write($"unsafe partial struct {structSymbol.Name}");

                var isBaseType = structSymbol.BaseType.Name == "ValueType";

                if (isBaseType)
                {
                    indentedWriter.WriteLine($": IAutoDeserialize<{structSymbol.Name}>");
                }

                indentedWriter.WriteLine("{");
                indentedWriter.Indent++;

                if (privateSupportedFieldsNeedMaxCount.Count > 0)
                {
                    {
                        indentedWriter.WriteLine("#region Array Logic");
                        {
                            {
                                indentedWriter.WriteLine("#region Array Logic - Delegates");
                                foreach (var fieldSymbol in privateSupportedFieldsNeedMaxCount)
                                {
                                    indentedWriter.WriteLine($"private delegate {fieldSymbol.Type.Name} Get{fieldSymbol.Type.Name}Delegate(ref {structSymbol.Name} @struct);");
                                }

                                indentedWriter.WriteLine("#endregion");
                            }
                            {
                                indentedWriter.WriteLine("#region Array Logic - Delegate Map");
                                foreach (var fieldSymbol in privateSupportedFieldsNeedMaxCount)
                                {
                                    indentedWriter.WriteLine($"private static readonly Get{fieldSymbol.Type.Name}Delegate[] Get{fieldSymbol.Type.Name}DelegateMap = {{");
                                    indentedWriter.Indent++;
                                    var attribute = fieldSymbol.GetAttributes().First(x => x.AttributeClass!.Equals(fieldCountAttributeSymbol, SymbolEqualityComparer.Default));

                                    var fieldMaxCount = (int)attribute.ConstructorArguments[1].Value!;
                                    for (var i = 0; i < fieldMaxCount; i++)
                                    {
                                        indentedWriter.WriteLine($"(ref {structSymbol.Name} @struct) => @struct._{fieldSymbol.Name}{i}{(i == fieldMaxCount - 1 ? string.Empty : ",")}");
                                    }

                                    indentedWriter.Indent--;
                                    indentedWriter.WriteLine("};");
                                }

                                indentedWriter.WriteLine("#endregion");
                            }
                            {
                                indentedWriter.WriteLine("#region Array Logic - Backing Fields");
                                foreach (var fieldSymbol in privateSupportedFieldsNeedMaxCount)
                                {
                                    var attribute = fieldSymbol.GetAttributes().First(x => x.AttributeClass!.Equals(fieldCountAttributeSymbol, SymbolEqualityComparer.Default));
                                    var fieldMaxCount = (int)attribute.ConstructorArguments[1].Value!;
                                    for (var i = 0; i < fieldMaxCount; i++)
                                    {
                                        indentedWriter.WriteLine($"private {fieldSymbol.Type.Name} _{fieldSymbol.Name}{i};");
                                    }
                                }

                                indentedWriter.WriteLine("#endregion");
                            }
                        }
                        indentedWriter.WriteLine("#endregion");
                    }
                }

                indentedWriter.WriteLine("#region - Expose Field as Properties");
                foreach (var fieldSymbol in privateSupportedFields)
                {
                    if (privateSupportedFieldsNeedMaxCount.Contains(fieldSymbol))
                        continue;

                    if (fieldSymbol.Type.TypeKind == TypeKind.Struct)
                    {
                        indentedWriter.WriteLine($"public {fieldSymbol.Type} {fieldSymbol.Name}Real => {fieldSymbol.Name};");
                        continue;
                    }

                    var pointerType = ((IPointerTypeSymbol)fieldSymbol.Type).PointedAtType;
                    //check is has a fieldCountAttribute
                    var fieldCountAttribute = fieldSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass!.Equals(fieldCountAttributeSymbol, SymbolEqualityComparer.Default));
                    if (fieldCountAttribute != null)
                    {
                        var fieldCount = (string)fieldCountAttribute!.ConstructorArguments[0].Value!;
                        var isInternalReferenceToPointer = privateSupportedFields.Any(symbol => symbol.Name == fieldCount);
                        indentedWriter.WriteLine($"public Span<{pointerType}> {fieldSymbol.Name}Real => new({fieldSymbol.Name}, {(isInternalReferenceToPointer ? "*" : string.Empty)}{fieldCount});");
                    }
                    else
                    {
                        indentedWriter.WriteLine($"public {pointerType} {fieldSymbol.Name}Real => *{fieldSymbol.Name};");
                    }
                }

                foreach (var fieldSymbol in privateSupportedFieldsNeedMaxCount)
                {
                    var fieldCountAttribute = fieldSymbol.GetAttributes().First(x => x.AttributeClass!.Equals(fieldCountAttributeSymbol, SymbolEqualityComparer.Default));
                    var fieldCount = (string)fieldCountAttribute.ConstructorArguments[0].Value!;
                    var isInternalReferenceToPointer = privateSupportedFields.Any(symbol => symbol.Name == fieldCount);
                    indentedWriter.WriteLine($"public {fieldSymbol.Type.Name} Get{fieldSymbol.Type.Name}(int index)");
                    indentedWriter.WriteLine("{");
                    indentedWriter.Indent++;
                    indentedWriter.WriteLine($"if (index < 0 || index >= Get{fieldSymbol.Type.Name}DelegateMap.Length || index > {(isInternalReferenceToPointer ? "*" : string.Empty)}{fieldCount})");
                    indentedWriter.WriteLine("{");
                    indentedWriter.Indent++;
                    indentedWriter.WriteLine("throw new ArgumentOutOfRangeException(nameof(index));");
                    indentedWriter.Indent--;
                    indentedWriter.WriteLine("}");
                    indentedWriter.WriteLine($"return Get{fieldSymbol.Type.Name}DelegateMap[index](ref this);");
                    indentedWriter.Indent--;
                    indentedWriter.WriteLine("}");
                }

                indentedWriter.WriteLine("#endregion");

                indentedWriter.WriteLine("#region Deserialize Logic");

                indentedWriter.WriteLine($"public static {structSymbol.Name} Deserialize(Span<byte> data)");
                indentedWriter.WriteLine("{");
                indentedWriter.Indent++;
                indentedWriter.WriteLine("var pointer = (byte*)Unsafe.AsPointer(ref data.GetPinnableReference());");
                indentedWriter.WriteLine("return Deserialize(ref pointer);");
                indentedWriter.Indent--;
                indentedWriter.WriteLine("}");
                indentedWriter.WriteLine();

                indentedWriter.WriteLine($"public static {structSymbol.Name} Deserialize(ArraySegment<byte> data)");
                indentedWriter.WriteLine("{");
                indentedWriter.Indent++;
                indentedWriter.WriteLine(" var pointer = (byte*)Unsafe.AsPointer(ref data.Array![data.Offset]);");
                indentedWriter.WriteLine("return Deserialize(ref pointer);");
                indentedWriter.Indent--;
                indentedWriter.WriteLine("}");
                indentedWriter.WriteLine();

                indentedWriter.WriteLine($"public static {structSymbol.Name} Deserialize(ref byte* bytePtr)");
                indentedWriter.WriteLine("{");
                indentedWriter.Indent++;
                indentedWriter.WriteLine($"{structSymbol.Name} deserialized = default;");
                indentedWriter.WriteLine();

                foreach (var fieldSymbol in privateSupportedFields)
                {
                    var serializeWhenAttribute = fieldSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass!.Equals(serializeWhenAttributeSymbol, SymbolEqualityComparer.Default));
                    var fixedLengthAttribute = fieldSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass!.Equals(fixedFieldLengthAttributeSymbol, SymbolEqualityComparer.Default));

                    if (serializeWhenAttribute != null)
                    {
                        var serializeWhen = serializeWhenAttribute != null ? (string)serializeWhenAttribute.ConstructorArguments[0].Value : string.Empty;
                        var isInternalReferenceToPointer = serializeWhenAttribute != null && privateSupportedFields.Any(symbol => symbol.Name == serializeWhen);
                        indentedWriter.WriteLine($"if ({(isInternalReferenceToPointer ? "*" : string.Empty)}{serializeWhen})");
                        indentedWriter.WriteLine("{");
                        indentedWriter.Indent++;
                    }

                    if (privateSupportedFieldsNeedMaxCount.Contains(fieldSymbol))
                    {
                        var fieldCountAttribute = fieldSymbol.GetAttributes().First(x => x.AttributeClass!.Equals(fieldCountAttributeSymbol, SymbolEqualityComparer.Default));
                        var fieldCount = (string)fieldCountAttribute.ConstructorArguments[0].Value!;
                        var fieldMaxCount = (int)fieldCountAttribute.ConstructorArguments[1].Value!;
                        var isInternalReferenceToPointer = privateSupportedFields.Any(symbol => symbol.Name == fieldCount);

                        indentedWriter.WriteLine($"for (var i = 0; i < {(isInternalReferenceToPointer ? "*" : string.Empty)}{fieldCount}; i++)");
                        indentedWriter.WriteLine("{");
                        indentedWriter.Indent++;
                        indentedWriter.WriteLine("switch(i)");
                        indentedWriter.WriteLine("{");
                        indentedWriter.Indent++;

                        for (int i = 0; i < fieldMaxCount; i++)
                        {
                            indentedWriter.WriteLine($"case {i}:");
                            indentedWriter.Indent++;
                            indentedWriter.WriteLine($"deserialized._{fieldSymbol.Name}{i} = {fieldSymbol.Type.Name}.Deserialize(ref bytePtr);");
                            indentedWriter.WriteLine("break;");
                            indentedWriter.Indent--;
                        }

                        indentedWriter.Indent--;
                        indentedWriter.WriteLine("}");
                        indentedWriter.Indent--;
                        indentedWriter.WriteLine("}");
                    }
                    else
                    {
                        if (fixedLengthAttribute != null)
                        {
                            indentedWriter.WriteLine($"var curPosition{fieldSymbol.Name} = bytePtr;");
                        }

                        if (fieldSymbol.Type.TypeKind == TypeKind.Struct)
                        {
                            indentedWriter.WriteLine($"{fieldSymbol.Type.Name} {fieldSymbol.Name} = {fieldSymbol.Type.Name}.Deserialize(ref bytePtr);");
                        }
                        else
                        {
                            indentedWriter.WriteLine($"DeserializeUtils.Read(ref bytePtr, out {fieldSymbol.Type.ToDisplayString()} {fieldSymbol.Name});");
                        }

                        if (fixedLengthAttribute != null)
                        {
                            var fixedLength = (int)fixedLengthAttribute.ConstructorArguments[0].Value!;
                            indentedWriter.WriteLine($"var writeLength{fieldSymbol.Name} = (int)(bytePtr - curPosition{fieldSymbol.Name});");
                            indentedWriter.WriteLine($"bytePtr += {fixedLength} - writeLength{fieldSymbol.Name};");
                        }

                        indentedWriter.WriteLine($"deserialized.{fieldSymbol.Name} = {fieldSymbol.Name};");
                    }

                    if (serializeWhenAttribute != null)
                    {
                        indentedWriter.Indent--;
                        indentedWriter.WriteLine("}");
                    }

                    indentedWriter.WriteLine();
                }

                indentedWriter.WriteLine("return deserialized;");
                indentedWriter.Indent--;
                indentedWriter.WriteLine("}");
                indentedWriter.WriteLine();

                indentedWriter.WriteLine("#endregion");

                indentedWriter.Indent--;
                indentedWriter.WriteLine("}");

                context.AddSource($"{structSymbol.Name}.AutoDeserializer.cs", SourceText.From(indentedWriter.InnerWriter.ToString(), Encoding.UTF8));
            }
        }
        catch (Exception e)
        {
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("AS0001", "AutoSerializer", e.Message, "AutoSerializer", DiagnosticSeverity.Error, true), Location.None));
        }
    }
}