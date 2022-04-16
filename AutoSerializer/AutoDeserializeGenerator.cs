using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AutoSerializer
{
    public class AutoDeserializeGenerator
    {
        public static void Generate(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
            {
                // nothing to do yet
                return;
            }

            try
            {
                var attributeSymbol =
                    compilation.GetTypeByMetadataName("AutoSerializer.Definitions.AutoDeserializeAttribute");

                var distinctClasses = classes.Distinct();

                var classSymbols = new List<INamedTypeSymbol>();
                foreach (ClassDeclarationSyntax cls in distinctClasses)
                {
                    var model = compilation.GetSemanticModel(cls.SyntaxTree);

                    var classSymbol = model.GetDeclaredSymbol(cls);
                    if (classSymbol?.GetAttributes().Any(ad => ad.AttributeClass?.Name == attributeSymbol?.Name) ?? false)
                    {
                        classSymbols.Add(classSymbol);
                    }
                }

                foreach (var classSymbol in classSymbols)
                {
                    var autoSerializerAssembly = Assembly.GetExecutingAssembly();

                    const string ResourceName = "AutoSerializer.Resources.AutoDeserializeClass.cs";
                    using (var resourceStream = autoSerializerAssembly.GetManifestResourceStream(ResourceName))
                    {
                        if (resourceStream == null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "ADG0001",
                                    "Invalid Resource",
                                    $"Cannot find {ResourceName} resource",
                                    "",
                                    DiagnosticSeverity.Error,
                                    true),
                                null));
                        }

                        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

                        var attributeData = classSymbol?.GetAttributes()
                            .First(ad => ad.AttributeClass?.Name == attributeSymbol?.Name);
                        var isDynamic = attributeData.NamedArguments.Length > 0 &&
                                        (bool) attributeData.NamedArguments.First().Value.Value;

                        var dynamicFieldContent = isDynamic ? "public byte[] DynamicData { get; set; }" : string.Empty;

                        var resourceContent = new StreamReader(resourceStream).ReadToEnd();
                        resourceContent = string.Format(resourceContent,
                            namespaceName,
                            classSymbol.Name,
                            GenerateDeserializeContent(context, attributeSymbol, classSymbol, isDynamic),
                            classSymbol.BaseType.Name != "Object" ? classSymbol.BaseType.ToString() : "IAutoDeserialize",
                            classSymbol.BaseType.Name != "Object" ? "override" : "virtual",
                            dynamicFieldContent);

                        context.AddSource($"{namespaceName}.{classSymbol.Name}.AutoDeserialize.g.cs",
                            SourceText.From(resourceContent, Encoding.UTF8));
                    }
                }
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "ADG0002",
                        "Unexpected Error",
                        $"Unexpected Error: {e}",
                        "",
                        DiagnosticSeverity.Error,
                        true),
                    null));
            }
        }

        private static string GenerateDeserializeContent(SourceProductionContext context, INamedTypeSymbol attribute,
            INamedTypeSymbol symbol, bool isDynamic)
        {
            var builder = new StringBuilder();

            var fieldSymbols = new List<IPropertySymbol>();
            foreach (var item in symbol.GetMembers())
            {
                if (item is IPropertySymbol itemProperty && itemProperty.DeclaredAccessibility == Accessibility.Public)
                {
                    fieldSymbols.Add((IPropertySymbol) item);
                }
            }

            if (symbol.BaseType.Name != "Object")
            {
                builder.Append('\t', 3).AppendLine($"base.Deserialize(in buffer, ref offset);").AppendLine();
            }

            foreach (var fieldSymbol in fieldSymbols)
            {
                var fieldLenAttrData = fieldSymbol.GetAttributes()
                    .FirstOrDefault(x => x.AttributeClass?.Name == "FieldLengthAttribute");
                var fixedLen = fieldLenAttrData?.ConstructorArguments.FirstOrDefault().Value;

                var serializeWhenAttrData = fieldSymbol.GetAttributes()
                    .FirstOrDefault(x => x.AttributeClass?.Name == "SerializeWhenAttribute");
                var serializeWhenExpression = serializeWhenAttrData?.ConstructorArguments.FirstOrDefault().Value;

                int tabSpace = 3;

                if (serializeWhenExpression != null)
                {
                    builder.Append('\t', tabSpace).AppendLine($"if ({serializeWhenExpression})");
                    builder.Append('\t', tabSpace).AppendLine("{");
                    tabSpace++;
                }

                var actualBytesFieldName = $"actualBytes_{fieldSymbol.Name}";
                var readBytesFieldName = $"readBytes_{fieldSymbol.Name}";
                var remainingBytesFieldName = $"remainingBytes_{fieldSymbol.Name}";

                if (fixedLen != null)
                {
                    builder.AppendLine();
                    builder.Append('\t', tabSpace).AppendLine($"int {actualBytesFieldName} = (int)offset;");
                }

                if (AutoSerializerUtils.NeedUseAutoSerializeOrDeserialize(fieldSymbol.Type))
                {
                    if (fieldSymbol.Type is IArrayTypeSymbol || AutoSerializerUtils.IsList(fieldSymbol.Type))
                    {
                        var isArray = fieldSymbol.Type is IArrayTypeSymbol;
                        var genericType = isArray ? ((IArrayTypeSymbol) fieldSymbol.Type).ElementType : ((INamedTypeSymbol) fieldSymbol.Type).TypeArguments[0];

                        if (fixedLen == null)
                        {
                            builder.Append('\t', tabSpace)
                                .AppendLine($"buffer.Read(ref offset, out int len_{fieldSymbol.Name});");
                            builder.Append('\t', tabSpace).AppendLine($"{fieldSymbol.Name} = new(len_{fieldSymbol.Name});");
                            builder.Append('\t', tabSpace).AppendLine($"for (var i = 0; i < len_{fieldSymbol.Name}; i++)");
                        }
                        else
                        {
                            builder.Append('\t', tabSpace).AppendLine($"{fieldSymbol.Name} = new({fixedLen});");
                            builder.Append('\t', tabSpace).AppendLine($"for (var i = 0; i < {fixedLen}; i++)");
                        }

                        builder.Append('\t', tabSpace).AppendLine("{");

                        builder.Append('\t', tabSpace + 1)
                            .AppendLine($"var instance_{fieldSymbol.Name} = new {genericType}();");

                        if (isArray)
                            builder.AppendLine($"{fieldSymbol.Name}[i] = instance_{fieldSymbol.Name};");
                        else
                            builder.AppendLine($"{fieldSymbol.Name}.Add(instance_{fieldSymbol.Name});");

                        builder.Append('\t', tabSpace + 1)
                            .AppendLine($"instance_{fieldSymbol.Name}.Deserialize(in buffer, ref offset);");

                        builder.Append('\t', tabSpace).AppendLine("}").AppendLine();
                    }
                    else
                    {
                        builder.Append('\t', tabSpace)
                            .AppendLine($"var instance_{fieldSymbol.Name} = new {fieldSymbol.Type}();");
                        builder.Append('\t', tabSpace).AppendLine($"{fieldSymbol.Name} = instance_{fieldSymbol.Name};");
                        builder.Append('\t', tabSpace)
                            .AppendLine($"instance_{fieldSymbol.Name}.Deserialize(in buffer, ref offset);");
                    }
                }
                else
                {
                    if (fieldSymbol.Type is INamedTypeSymbol {EnumUnderlyingType: { }} nameSymbol)
                    {
                        builder.Append('\t', tabSpace)
                            .AppendLine(
                                $"buffer.Read(ref offset, out {nameSymbol.EnumUnderlyingType} read_{fieldSymbol.Name});");
                        builder.Append('\t', tabSpace)
                            .AppendLine(
                                $"{fieldSymbol.Name} = ({fieldSymbol.Type})Enum.Parse(typeof({fieldSymbol.Type}), read_{fieldSymbol.Name}.ToString());");
                    }
                    else
                    {
                        if ((fieldSymbol.Type.ToString() == "string" || fieldSymbol.Type is IArrayTypeSymbol) && fixedLen != null)
                        {
                            builder.Append('\t', tabSpace)
                                .AppendLine($"buffer.Read(ref offset, {fixedLen}, out {fieldSymbol.Type} read_{fieldSymbol.Name});");
                        }
                        else
                        {
                            builder.Append('\t', tabSpace)
                                .AppendLine($"buffer.Read(ref offset, out {fieldSymbol.Type} read_{fieldSymbol.Name});");
                        }

                        builder.Append('\t', tabSpace).AppendLine($"{fieldSymbol.Name} = read_{fieldSymbol.Name};");
                    }
                }

                if (fixedLen != null)
                {
                    builder.Append('\t', tabSpace)
                        .AppendLine($"int {readBytesFieldName} = (int)(offset - {actualBytesFieldName});");
                    builder.Append('\t', tabSpace)
                        .AppendLine($"int {remainingBytesFieldName} = {fixedLen} - {readBytesFieldName};");

                    builder.Append('\t', tabSpace).AppendLine($"if ({remainingBytesFieldName} > 0)");
                    builder.Append('\t', ++tabSpace).AppendLine($"buffer.Read(ref offset, {remainingBytesFieldName}, out byte[] {remainingBytesFieldName}_data);")
                        .AppendLine();
                }

                if (serializeWhenExpression != null)
                {
                    builder.Append('\t', tabSpace - 1).AppendLine("}").AppendLine();
                }
            }

            if (isDynamic)
            {
                builder.Append('\t', 3)
                    .AppendLine($"buffer.Read(ref offset, buffer.Count - (offset - buffer.Offset), out byte[] read_dynamicData);");
                builder.Append('\t', 3).AppendLine($"DynamicData = read_dynamicData;");
            }

            return builder.ToString();
        }
    }
}