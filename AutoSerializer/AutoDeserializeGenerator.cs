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
    public class AutoDeserializeGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG
            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();
            //}
#endif

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
                compilation.GetTypeByMetadataName("AutoSerializer.Definitions.AutoDeserializeAttribute");

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

                const string ResourceName = "AutoSerializer.Resources.AutoDeserializeClass.cs";
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

                    var attributeData = classSymbol?.GetAttributes().First(ad => ad.AttributeClass?.Name == attributeSymbol?.Name);
                    var isDynamic = attributeData.NamedArguments.Length > 0 && (bool)attributeData.NamedArguments.First().Value.Value;

                    var dynamicFieldContent = isDynamic ? "public byte[] DynamicData { get; set; }" : string.Empty;

                    var resourceContent = new StreamReader(resourceStream).ReadToEnd();
                    resourceContent = string.Format(resourceContent,
                        namespaceName,
                        classSymbol.Name,
                        GenerateDeserializeContent(context, attributeSymbol, classSymbol, isDynamic),
                        classSymbol.BaseType.Name != "Object" ? classSymbol.BaseType.ToString() : "IAutoDeserialize",
                        classSymbol.BaseType.Name != "Object" ? "override" : "virtual",
                        dynamicFieldContent);

                    context.AddSource($"{namespaceName}.{classSymbol.Name}.g.cs", SourceText.From(resourceContent, Encoding.UTF8));
                }
            }
        }

        private static string GenerateDeserializeContent(GeneratorExecutionContext context, INamedTypeSymbol attribute, INamedTypeSymbol symbol, bool isDynamic)
        {
            var builder = new StringBuilder();

            var fieldSymbols = new List<IPropertySymbol>();
            foreach (var item in symbol.GetMembers())
            {
                if (item is IPropertySymbol itemProperty && itemProperty.DeclaredAccessibility == Accessibility.Public)
                {
                    fieldSymbols.Add((IPropertySymbol)item);
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

                if (fieldSymbol.Type.IsValueType)
                {
                    var nameSymbol = (INamedTypeSymbol)fieldSymbol.Type;
                    if (nameSymbol.EnumUnderlyingType != null)
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
                        builder.Append('\t', tabSpace)
                            .AppendLine($"buffer.Read(ref offset, out {fieldSymbol.Type} read_{fieldSymbol.Name});");
                        builder.Append('\t', tabSpace).AppendLine($"{fieldSymbol.Name} = read_{fieldSymbol.Name};");
                    }
                }
                else if (fieldSymbol.Type.ToString() == "string" || fieldSymbol.Type.ToString().EndsWith("[]"))
                {
                    if (fieldSymbol.Type.ToString().EndsWith("[]") && fixedLen == null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "ADG0002",
                                "Missing [FieldLengthAttribute]",
                                "Property {0} of type {1} miss [FieldLengthAttribute]",
                                "type-not-supported",
                                DiagnosticSeverity.Error,
                                true), fieldSymbol.Locations.FirstOrDefault(), fieldSymbol.Name, fieldSymbol.Type));
                        continue;
                    }

                    if (fixedLen == null)
                    {
                        builder.Append('\t', tabSpace)
                            .AppendLine($"buffer.Read(ref offset, out {fieldSymbol.Type} read_{fieldSymbol.Name});");
                        builder.Append('\t', tabSpace).AppendLine($"{fieldSymbol.Name} = read_{fieldSymbol.Name};");
                    }
                    else
                    {
                        builder.Append('\t', tabSpace)
                            .AppendLine(
                                $"buffer.Read(ref offset, {fixedLen}, out {fieldSymbol.Type} read_{fieldSymbol.Name});")
                            .AppendLine();
                        builder.Append('\t', tabSpace).AppendLine($"{fieldSymbol.Name} = read_{fieldSymbol.Name};")
                            .AppendLine();
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
                                "ADG0001",
                                "List is of a type not supported",
                                "List {0} of type {1} it's not supported",
                                "type-not-supported",
                                DiagnosticSeverity.Error,
                                true), fieldSymbol.Locations.FirstOrDefault(), fieldSymbol.Name, genericType));
                        continue;
                    }

                    builder.Append('\t', tabSpace).AppendLine($"{fieldSymbol.Name} = new({fixedLen});");

                    if (fixedLen == null)
                    {
                        builder.Append('\t', tabSpace).AppendLine($"buffer.Read(ref offset, out int len_{fieldSymbol.Name});");
                        builder.Append('\t', tabSpace).AppendLine($"for (var i = 0; i < len_{fieldSymbol.Name}; i++)");
                    }
                    else
                    {
                        builder.Append('\t', tabSpace).AppendLine($"for (var i = 0; i < {fixedLen}; i++)");
                    }

                    builder.Append('\t', tabSpace).AppendLine("{");

                    if (genericType.IsValueType)
                    {
                        builder.Append('\t', tabSpace + 1)
                            .AppendLine($"buffer.Read(ref offset, out {genericType} read_{fieldSymbol.Name});");
                        builder.Append('\t', tabSpace + 1)
                            .AppendLine($"{fieldSymbol.Name}.Add(read_{fieldSymbol.Name});");
                    }
                    else
                    {
                        builder.Append('\t', tabSpace + 1)
                            .AppendLine($"var instance_{fieldSymbol.Name} = new {genericType}();");
                        builder.Append('\t', tabSpace + 1)
                            .AppendLine($"{fieldSymbol.Name}.Add(instance_{fieldSymbol.Name});");
                        builder.Append('\t', tabSpace + 1)
                            .AppendLine($"instance_{fieldSymbol.Name}.Deserialize(in buffer, ref offset);");
                    }

                    builder.Append('\t', tabSpace).AppendLine("}").AppendLine();
                }
                else if (fieldSymbol.Type.GetAttributes().Any(x => x.AttributeClass?.Name == attribute?.Name))
                {
                    builder.Append('\t', tabSpace)
                        .AppendLine($"var instance_{fieldSymbol.Name} = new {fieldSymbol.Type}();");
                    builder.Append('\t', tabSpace).AppendLine($"{fieldSymbol.Name} = instance_{fieldSymbol.Name};");
                    builder.Append('\t', tabSpace)
                        .AppendLine($"instance_{fieldSymbol.Name}.Deserialize(in buffer, ref offset);");
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "ADG0001",
                            "Type not supported",
                            "Property {0} of type {1} it's not supported",
                            "type-not-supported",
                            DiagnosticSeverity.Error,
                            true), fieldSymbol.Locations.FirstOrDefault(), fieldSymbol.Name, fieldSymbol.Type));
                }

                if (serializeWhenExpression != null)
                {
                    builder.Append('\t', tabSpace - 1).AppendLine("}").AppendLine();
                }
            }

            if (isDynamic)
            {
                builder.Append('\t', 3).AppendLine($"buffer.Read(ref offset, buffer.Count - offset, out byte[] read_dynamicData);");
                builder.Append('\t', 3).AppendLine($"DynamicData = read_dynamicData;");
            }

            return builder.ToString();
        }
    }
}