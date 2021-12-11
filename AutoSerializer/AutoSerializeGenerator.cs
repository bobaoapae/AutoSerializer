using System;
using System.Collections.Generic;
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
    [Generator]
    public class AutoSerializeGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            //Debugger.Launch();
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var rand = new Random();
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            {
                return;
            }

            var compilation = context.Compilation;

            var attributeSymbol =
                compilation.GetTypeByMetadataName("AutoSerializer.Definitions.AutoSerializeAttribute");

            var classSymbols = new List<INamedTypeSymbol>();
            foreach (ClassDeclarationSyntax cls in receiver.CandidateClasses)
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

                const string ResourceName = "AutoSerializer.Resources.AutoSerializeClass.cs";
                using (var resourceStream = autoSerializerAssembly.GetManifestResourceStream(ResourceName))
                {
                    if (resourceStream == null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "AG0001",
                                "Invalid Resource",
                                $"Cannot find {ResourceName} resource",
                                "",
                                DiagnosticSeverity.Error,
                                true),
                            null));
                    }

                    var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

                    var resourceContent = new StreamReader(resourceStream).ReadToEnd();
                    resourceContent = string.Format(resourceContent, namespaceName, classSymbol.Name,
                        GenerateSerializeContent(context, attributeSymbol, classSymbol),
                        classSymbol.BaseType.Name != "Object" ? classSymbol.BaseType.ToString() : "IAutoSerialize",
                        classSymbol.BaseType.Name != "Object" ? "override" : "virtual");

                    context.AddSource($"{namespaceName}.{classSymbol.Name}.g.cs", SourceText.From(resourceContent, Encoding.UTF8));
                }
            }
        }

        private static string GenerateSerializeContent(GeneratorExecutionContext context, INamedTypeSymbol attribute,
            INamedTypeSymbol symbol)
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

            if (symbol.BaseType.Name != "Object")
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

                if (fieldSymbol.Type.IsValueType)
                {
                    var nameSymbol = (INamedTypeSymbol)fieldSymbol.Type;
                    builder
                        .Append('\t', tabSpace)
                        .AppendLine(
                            $"stream.ExWrite({(nameSymbol.EnumUnderlyingType != null ? $"({nameSymbol.EnumUnderlyingType})" : "") + fieldSymbol.Name});");
                }
                else if (fieldSymbol.Type.ToString() == "string" || fieldSymbol.Type.ToString().EndsWith("[]"))
                {
                    if (fieldSymbol.Type.ToString().EndsWith("[]") && fixedLen == null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "ASG0002",
                                "Missing [FieldLengthAttribute]",
                                "Property {0} of type {1} miss [FieldLengthAttribute]",
                                "type-not-supported",
                                DiagnosticSeverity.Error,
                                true), fieldSymbol.Locations.FirstOrDefault(), fieldSymbol.Name, fieldSymbol.Type));
                        continue;
                    }

                    if (fixedLen == null)
                    {
                        builder
                            .Append('\t', tabSpace)
                            .AppendLine(
                                $"stream.ExWrite({fieldSymbol.Name});");
                    }
                    else
                    {
                        builder
                            .Append('\t', tabSpace)
                            .AppendLine($"stream.ExWrite({fieldSymbol.Name}, {fieldSymbol.Name}?.Length ?? 0);");
                    }
                }
                else if (fieldSymbol.Type.Name == "List")
                {
                    var genericType = ((INamedTypeSymbol)fieldSymbol.Type).TypeArguments[0];


                    if (!genericType.IsValueType &&
                        !genericType.GetAttributes().Any(x => x.AttributeClass?.Name == attribute?.Name))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "ASG0001",
                                "List is of a type not supported",
                                "List {0} of type {1} it's not supported",
                                "type-not-supported",
                                DiagnosticSeverity.Error,
                                true), fieldSymbol.Locations.FirstOrDefault(), fieldSymbol.Name, genericType));
                        continue;
                    }

                    if (fixedLen == null)
                    {
                        builder.Append('\t', tabSpace).AppendLine($"stream.ExWrite({fieldSymbol.Name}?.Count ?? 0);");
                    }

                    builder.AppendLine();

                    builder.Append('\t', tabSpace).AppendLine($"if ({fieldSymbol.Name} != null)");
                    builder.Append('\t', tabSpace++).AppendLine("{");

                    builder.Append('\t', tabSpace).AppendLine($"for (var i = 0; i < {fieldSymbol.Name}.Count; i++)");
                    builder.Append('\t', tabSpace++).AppendLine("{");

                    builder.Append('\t', tabSpace).AppendLine(genericType.IsValueType
                        ? $"stream.ExWrite({fieldSymbol.Name}[i]);"
                        : $"{fieldSymbol.Name}[i]?.Serialize(stream);");

                    builder.Append('\t', --tabSpace).AppendLine("}");

                    builder.Append('\t', --tabSpace).AppendLine("}").AppendLine();
                }
                else if (fieldSymbol.Type.GetAttributes().Any(x => x.AttributeClass?.Name == attribute?.Name))
                {
                    builder.Append('\t', tabSpace).AppendLine($"{fieldSymbol.Name}.Serialize(stream);").AppendLine();
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "ASG0001",
                            "Type not supported",
                            "Property {0} of type {1} it's not supported",
                            "type-not-supported",
                            DiagnosticSeverity.Error,
                            true), fieldSymbol.Locations.FirstOrDefault(), fieldSymbol.Name, fieldSymbol.Type));
                }

                if (fixedLen != null)
                {
                    builder.Append('\t', tabSpace)
                        .AppendLine($"int {writedBytesFieldName} = (int)(stream.Length - {actualBytesFieldName});");
                    builder.Append('\t', tabSpace)
                        .AppendLine($"int {remainingBytesFieldName} = {fixedLen} - {writedBytesFieldName};");

                    builder.Append('\t', tabSpace).AppendLine($"if ({remainingBytesFieldName} > 0)");
                    builder.Append('\t', ++tabSpace).AppendLine($"stream.Skip({remainingBytesFieldName});")
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