using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AutoSerializer;

public class JsonViewerGenerator
{
    public static string GenerateSerializeJsonContent(SourceProductionContext context, INamedTypeSymbol attributeSymbol, INamedTypeSymbol classSymbol)
    {
        return "";
        using var textWriter = new StringWriter();
        using var indentedTextWriter = new IndentedTextWriter(textWriter, "\t");
        indentedTextWriter.Indent = 3;

        var fieldSymbols = new List<IPropertySymbol>();
        foreach (var item in classSymbol.GetMembers())
        {
            if (item is IPropertySymbol { DeclaredAccessibility: Accessibility.Public } itemProperty)
            {
                fieldSymbols.Add(itemProperty);
            }
        }

        if (classSymbol.BaseType!.Name != "Object")
        {
            indentedTextWriter.WriteLine("base.WriteToJsonView(jObject);");
            indentedTextWriter.WriteLine();
        }

        foreach (var fieldSymbol in fieldSymbols)
        {
            indentedTextWriter.WriteLine("{");
            indentedTextWriter.Indent++;

            var serializeWhenExpression = fieldSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "SerializeWhenAttribute")?.ConstructorArguments.FirstOrDefault().Value?.ToString();
            var fieldCountAttrData = fieldSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "FieldCountAttribute");
            var fieldLenAttrData = fieldSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "FieldLengthAttribute");
            var fixedLenOrCount = fieldCountAttrData?.ConstructorArguments.FirstOrDefault().Value ?? fieldLenAttrData?.ConstructorArguments.FirstOrDefault().Value;

            if (serializeWhenExpression != null)
            {
                indentedTextWriter.WriteLine($"if ({serializeWhenExpression})");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;
            }

            var fieldType = fieldSymbol.Type;

            var isArrayOrList = fieldSymbol.Type is IArrayTypeSymbol || AutoSerializerUtils.IsList(fieldSymbol.Type) || AutoSerializerUtils.IsMemory(fieldSymbol.Type);

            if (fieldType is IArrayTypeSymbol arrayTypeSymbol)
            {
                fieldType = arrayTypeSymbol.ElementType;
            }
            else if (AutoSerializerUtils.IsList(fieldType))
            {
                fieldType = ((INamedTypeSymbol)fieldType).TypeArguments.First();
            }

            var isEnum = fieldType is INamedTypeSymbol { EnumUnderlyingType: { } };
            var isPrimitive = fieldType.SpecialType != SpecialType.None;
            var implicitCastToPrimitive = isPrimitive
                ? null
                : fieldType.GetMembers().FirstOrDefault(x => x is IMethodSymbol { MethodKind: MethodKind.Conversion } methodSymbol && methodSymbol.ReturnType.SpecialType != SpecialType.None) as IMethodSymbol;
            var isImplicitCastToPrimitive = implicitCastToPrimitive != null;
            var implicitCastToPrimitiveType = implicitCastToPrimitive?.ReturnType;
            var enumUnderlyingType = isEnum ? ((INamedTypeSymbol)fieldType).EnumUnderlyingType : null;

            indentedTextWriter.WriteLine("dynamic jPropertyDescription = new JObject();");
            indentedTextWriter.WriteLine("jPropertyDescription.Type = \"{0}\";", isImplicitCastToPrimitive ? implicitCastToPrimitiveType.ToDisplayString() : fieldType.ToDisplayString());
            indentedTextWriter.WriteLine("jPropertyDescription.IsPrimitive = {0};", isPrimitive ? "true" : "false");
            indentedTextWriter.WriteLine("jPropertyDescription.IsArray = {0};", isArrayOrList ? "true" : "false");
            indentedTextWriter.WriteLine("jPropertyDescription.IsEnum = {0};", isEnum ? "true" : "false");
            indentedTextWriter.WriteLine("jPropertyDescription.EnumUnderlyingType = \"{0}\";", isEnum ? enumUnderlyingType.ToDisplayString() : "");
            indentedTextWriter.WriteLine("jPropertyDescription.FixedLen = {0};", fixedLenOrCount != null ? fixedLenOrCount.ToString() : "\"\"");

            if (isArrayOrList)
            {
                indentedTextWriter.WriteLine("jPropertyDescription.Value = new JArray();");
                indentedTextWriter.WriteLine("foreach (var item in {0}{1})", fieldSymbol.Name, AutoSerializerUtils.IsMemory(fieldSymbol.Type) ? ".Span" : "");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;
                if (isPrimitive || isImplicitCastToPrimitive)
                {
                    if (isImplicitCastToPrimitive)
                    {
                        indentedTextWriter.WriteLine("jPropertyDescription.Value.Add(({0})item);", implicitCastToPrimitive.ReturnType.ToDisplayString());
                    }
                    else
                    {
                        indentedTextWriter.WriteLine("jPropertyDescription.Value.Add(item);");
                    }
                }
                else
                {
                    indentedTextWriter.WriteLine("dynamic jArrayItem = new JObject();");
                    if (isEnum)
                    {
                        indentedTextWriter.WriteLine("jArrayItem.Value = item.ToString();");
                        indentedTextWriter.WriteLine("jArrayItem.EnumUnderlyingValue = ({0})item;", enumUnderlyingType.ToDisplayString());
                    }
                    else
                    {
                        indentedTextWriter.WriteLine("item.WriteToJsonView(jArrayItem);");
                    }

                    indentedTextWriter.WriteLine("jPropertyDescription.Value.Add(jArrayItem);");
                }

                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLine("}");
            }
            else
            {
                if (isPrimitive || isImplicitCastToPrimitive)
                {
                    if (isImplicitCastToPrimitive)
                    {
                        indentedTextWriter.WriteLine("jPropertyDescription.Value = ({0}){1};", implicitCastToPrimitive.ReturnType.ToDisplayString(), fieldSymbol.Name);
                    }
                    else
                    {
                        indentedTextWriter.WriteLine("jPropertyDescription.Value = {0};", fieldSymbol.Name);
                    }
                }
                else
                {
                    indentedTextWriter.WriteLine("dynamic jProperty = new JObject();");
                    if (isEnum)
                    {
                        indentedTextWriter.WriteLine("jProperty.Value = {0}.ToString();", fieldSymbol.Name);
                        indentedTextWriter.WriteLine("jProperty.EnumUnderlyingValue = ({0}){1};", enumUnderlyingType.ToDisplayString(), fieldSymbol.Name);
                    }
                    else
                    {
                        indentedTextWriter.WriteLine("{0}.WriteToJsonView(jProperty);", fieldSymbol.Name);
                    }

                    indentedTextWriter.WriteLine("jPropertyDescription.Value = jProperty;");
                }
            }

            indentedTextWriter.WriteLine("jObject.Add(nameof({0}), jPropertyDescription);", fieldSymbol.Name);

            indentedTextWriter.Indent--;
            indentedTextWriter.WriteLine("}");

            if (serializeWhenExpression != null)
            {
                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLine("}");
            }
        }


        return textWriter.ToString();
    }
}