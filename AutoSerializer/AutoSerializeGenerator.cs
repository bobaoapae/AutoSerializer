using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AutoSerializer
{
    public class AutoSerializeGenerator
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
                var autoSerializerAssembly = Assembly.GetExecutingAssembly();

                var attributeSymbol = compilation.GetTypeByMetadataName("AutoSerializer.Definitions.AutoSerializeAttribute");
                var autoSerializeTemplate = AutoSerializerUtils.GetResource(autoSerializerAssembly, context, "AutoSerializeClass");
                var writeToJsonMethodTemplate = AutoSerializerUtils.GetResource(autoSerializerAssembly, context, "WriteToJsonMethod");

                foreach (var classSymbol in classes)
                {
                    var isBaseClass = classSymbol.BaseType.Name == "Object";

                    var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

                    var writeToJsonMethod = AutoSerializerUtils.IsBothAutoSerializeAndDeserialize(classSymbol)
                        ? ""
                        : string.Format(writeToJsonMethodTemplate, !isBaseClass ? "override" : "virtual", JsonViewerGenerator.GenerateSerializeJsonContent(context, attributeSymbol, classSymbol));

                    var resourceContent = string.Format(autoSerializeTemplate,
                        namespaceName,
                        classSymbol.Name,
                        isBaseClass ? " : IAutoSerialize" : "",
                        isBaseClass ? "virtual" : "override",
                        GenerateSerializeContent(context, attributeSymbol, classSymbol),
                        writeToJsonMethod);

                    context.AddSource($"{namespaceName}.{classSymbol.Name}.AutoSerialize.g.cs", SourceText.From(resourceContent, Encoding.UTF8));
                }
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "ASG0002",
                        "Unexpected Error",
                        $"Unexpected Error: {e}",
                        "",
                        DiagnosticSeverity.Error,
                        true),
                    null));
            }
        }

        private static string GenerateSerializeContent(SourceProductionContext context, INamedTypeSymbol attribute, INamedTypeSymbol symbol)
        {
            var builder = new StringBuilder();

            var fieldSymbols = new List<IPropertySymbol>();
            foreach (var item in symbol.GetMembers())
            {
                if (item is IPropertySymbol itemProperty && itemProperty.DeclaredAccessibility == Accessibility.Public)
                {
                    fieldSymbols.Add(itemProperty);
                }
            }

            if (symbol.BaseType!.Name != "Object")
            {
                builder.Append('\t', 3).AppendLine($"base.Serialize(stream);").AppendLine();
            }

            foreach (var fieldSymbol in fieldSymbols)
            {
                var fieldLenAttrData = fieldSymbol.GetAttributes()
                    .FirstOrDefault(x => x.AttributeClass?.Name == "FieldLengthAttribute");
                var fixedLen = fieldLenAttrData?.ConstructorArguments.FirstOrDefault().Value;

                var serializeWhenAttrData = fieldSymbol.GetAttributes()
                    .FirstOrDefault(x => x.AttributeClass?.Name == "SerializeWhenAttribute");
                var serializeWhenExpression = serializeWhenAttrData?.ConstructorArguments.FirstOrDefault().Value;

                var actualBytesFieldName = $"actualBytes_{fieldSymbol.Name}";
                var writedBytesFieldName = $"writedBytes_{fieldSymbol.Name}";
                var remainingBytesFieldName = $"remainingBytes_{fieldSymbol.Name}";

                int tabSpace = 3;

                if (serializeWhenExpression != null)
                {
                    builder.Append('\t', tabSpace).AppendLine($"if ({serializeWhenExpression})");
                    builder.Append('\t', tabSpace++).AppendLine("{");
                }

                if (fixedLen != null)
                {
                    builder.AppendLine();
                    builder.Append('\t', tabSpace).AppendLine($"int {actualBytesFieldName} = (int)stream.Length;");
                }

                if (fieldSymbol.Type.ToString() == "string" || fieldSymbol.Type is IArrayTypeSymbol || AutoSerializerUtils.IsList(fieldSymbol.Type))
                {
                    var fieldCountAttrData = fieldSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "FieldCountAttribute");
                    var fixedCount = fieldCountAttrData?.ConstructorArguments.FirstOrDefault().Value;

                    if (fixedLen == null && fixedCount == null)
                    {
                        if (fieldSymbol.Type.ToString() == "string")
                        {
                            builder.Append('\t', tabSpace).AppendLine($"stream.ExWrite({fieldSymbol.Name} == null ? 0 : System.Text.Encoding.UTF8.GetByteCount({fieldSymbol.Name}));");
                        }
                        else
                        {
                            var sizeProperty = fieldSymbol.Type is IArrayTypeSymbol ? "Length" : "Count";

                            builder.Append('\t', tabSpace).AppendLine($"stream.ExWrite({fieldSymbol.Name}?.{sizeProperty} ?? 0);");
                        }
                    }
                }

                if (fieldSymbol.Type is INamedTypeSymbol { EnumUnderlyingType: { } } nameSymbol)
                {
                    builder.Append('\t', tabSpace).AppendLine($"stream.ExWrite({(nameSymbol.EnumUnderlyingType != null ? $"({nameSymbol.EnumUnderlyingType})" : "") + fieldSymbol.Name});");
                }
                else
                {
                    //TODO: support collection of enum
                    builder.Append('\t', tabSpace).AppendLine($"stream.ExWrite({fieldSymbol.Name});");
                    if (fieldSymbol.Type is IArrayTypeSymbol || AutoSerializerUtils.IsList(fieldSymbol.Type))
                    {
                        var fieldCountAttrData = fieldSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "FieldCountAttribute");
                        var fixedCount = fieldCountAttrData?.ConstructorArguments.FirstOrDefault().Value;

                        if (fixedCount != null)
                        {
                            var sizeProperty = fieldSymbol.Type is IArrayTypeSymbol ? "Length" : "Count";
                            var genericType = fieldSymbol.Type is IArrayTypeSymbol arrayTypeSymbol ? arrayTypeSymbol.ElementType : ((INamedTypeSymbol)fieldSymbol.Type).TypeArguments.First();
                            var isGenericEnum = genericType is INamedTypeSymbol { EnumUnderlyingType: { } };
                            var isGenericObject = !isGenericEnum && (genericType is INamedTypeSymbol { TypeKind: TypeKind.Class } && genericType.ToString() != "string");
                            var isString = genericType.ToString() == "string";

                            builder.Append('\t', tabSpace).AppendLine($"if ({fieldSymbol.Name} == null || {fieldSymbol.Name}.{sizeProperty} < {fixedCount})");
                            builder.Append('\t', tabSpace++).AppendLine("{");

                            if (isGenericObject)
                            {
                                builder.Append('\t', tabSpace).AppendLine($"var trashObjForFixedLen = {genericType}.Create();");
                            }
                            else if (isGenericEnum || !isString)
                            {
                                builder.Append('\t', tabSpace).AppendLine($"var trashObjForFixedLen = ({genericType})0;");
                            }
                            else
                            {
                                builder.Append('\t', tabSpace).AppendLine("var trashObjForFixedLen = string.Empty;;");
                            }

                            builder.Append('\t', tabSpace).AppendLine($"for (var i = 0; i < {fixedCount} - ({fieldSymbol.Name}?.{sizeProperty} ?? 0); i++)");
                            builder.Append('\t', tabSpace++).AppendLine("{");
                            builder.Append('\t', tabSpace).AppendLine($"stream.ExWrite(trashObjForFixedLen);");
                            builder.Append('\t', --tabSpace).AppendLine("}");

                            if (isGenericObject)
                            {
                                builder.Append('\t', tabSpace).AppendLine($"trashObjForFixedLen.Dispose();");
                            }

                            builder.Append('\t', --tabSpace).AppendLine("}");
                        }
                    }
                }

                if (fixedLen != null)
                {
                    builder.Append('\t', tabSpace)
                        .AppendLine($"int {writedBytesFieldName} = (int)(stream.Length - {actualBytesFieldName});");
                    builder.Append('\t', tabSpace)
                        .AppendLine($"int {remainingBytesFieldName} = {fixedLen} - {writedBytesFieldName};");

                    builder.Append('\t', tabSpace).AppendLine($"if ({remainingBytesFieldName} > 0)");
                    builder.Append('\t', ++tabSpace).AppendLine($"stream.ExSkip({remainingBytesFieldName});")
                        .AppendLine();
                }

                if (serializeWhenExpression != null)
                {
                    builder.Append('\t', --tabSpace).AppendLine("}").AppendLine();
                }
            }

            return builder.ToString();
        }
    }
}