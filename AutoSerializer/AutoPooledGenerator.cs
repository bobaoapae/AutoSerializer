using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AutoSerializer
{
    public class AutoPooledGenerator
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
                var autoPooledClass = AutoSerializerUtils.GetResource(autoSerializerAssembly, context, "AutoPooledClass");
                var disposeTemplate = AutoSerializerUtils.GetResource(autoSerializerAssembly, context, "DisposeTemplate");


                foreach (var classSymbol in classes.Distinct())
                {
                    var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

                    var isBaseClass = classSymbol.BaseType!.Name == "Object";

                    var overrideOrVirtual = !isBaseClass ? "override" : "virtual";
                    var @new = !isBaseClass ? "new" : "";
                    var disposeMethod = isBaseClass ? disposeTemplate : "";

                    var pooledClassContent = string.Format(autoPooledClass,
                        namespaceName,
                        classSymbol.Name,
                        !isBaseClass ? "" : " : IDisposable",
                        overrideOrVirtual,
                        @new,
                        GenerateInitializePooledLists(context, classSymbol),
                        GenerateCleanAllProperties(context, classSymbol),
                        disposeMethod
                    );

                    context.AddSource($"{namespaceName}.{classSymbol.Name}.AutoPooled.g.cs", SourceText.From(pooledClassContent, Encoding.UTF8));
                }
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "ADG0002",
                        "Unexpected Error",
                        $"Unexpected Error: {e} - {e.StackTrace}",
                        "",
                        DiagnosticSeverity.Error,
                        true),
                    null));
            }
        }

        private static string GenerateInitializePooledLists(SourceProductionContext context, INamedTypeSymbol classSymbol)
        {
            var builder = new StringBuilder();

            foreach (var item in classSymbol.GetMembers())
            {
                if (item is IPropertySymbol itemProperty && itemProperty.DeclaredAccessibility == Accessibility.Public)
                {
                    if (AutoSerializerUtils.IsPooledList(itemProperty.Type))
                    {
                        var genericType = ((INamedTypeSymbol)itemProperty.Type).TypeArguments.First();
                        builder.Append('\t', 3).AppendLine($"{itemProperty.Name} = new Collections.Pooled.PooledList<{genericType}>();");
                    }
                }
            }

            return builder.ToString();
        }

        private static string GenerateCleanAllProperties(SourceProductionContext sourceProductionContext, INamedTypeSymbol symbol)
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
                builder.Append('\t', 3).AppendLine("base.CleanObject();").AppendLine();
            }

            foreach (var fieldSymbol in fieldSymbols)
            {
                var isListOrArray = fieldSymbol.Type is IArrayTypeSymbol || AutoSerializerUtils.IsList(fieldSymbol.Type);
                //check if it's a object or a value type that is not a enum nor a string
                var isEnum = fieldSymbol.Type is INamedTypeSymbol { EnumUnderlyingType: { } };
                var isObject = !isEnum && (fieldSymbol.Type is INamedTypeSymbol { TypeKind: TypeKind.Class } && fieldSymbol.Type.ToString() != "string");

                if (isListOrArray)
                {
                    //get array or list generic type
                    var genericType = fieldSymbol.Type is IArrayTypeSymbol arraySymbol ? arraySymbol.ElementType : ((INamedTypeSymbol)fieldSymbol.Type).TypeArguments[0];
                    //check if it's a object or a value type that is not a enum nor a string
                    var isGenericEnum = genericType is INamedTypeSymbol { EnumUnderlyingType: { } };
                    var isGenericObject = !isGenericEnum && (genericType is INamedTypeSymbol { TypeKind: TypeKind.Class } && genericType.ToString() != "string");

                    if (isGenericObject)
                    {
                        var sizeProperty = fieldSymbol.Type is IArrayTypeSymbol ? "Length" : "Count";
                        builder.Append('\t', 3).AppendLine($"if ({fieldSymbol.Name} != null)");
                        builder.Append('\t', 3).AppendLine("{");
                        builder.Append('\t', 4).AppendLine($"for (int i = 0; i < {fieldSymbol.Name}.{sizeProperty}; i++)");
                        builder.Append('\t', 4).AppendLine("{");
                        builder.Append('\t', 5).AppendLine($"{fieldSymbol.Name}[i]?.Dispose();");
                        builder.Append('\t', 4).AppendLine("}");
                        builder.Append('\t', 3).AppendLine("}");
                    }

                    if (AutoSerializerUtils.IsPooledList(fieldSymbol.Type))
                    {
                        builder.Append('\t', 3).AppendLine($"{fieldSymbol.Name}?.Clear();");
                        builder.Append('\t', 3).AppendLine($"{fieldSymbol.Name}?.Dispose();");
                    }
                }
                else if (isObject)
                {
                    builder.Append('\t', 3).AppendLine($"{fieldSymbol.Name}?.Dispose();");
                }

                builder.Append('\t', 3).AppendLine($"{fieldSymbol.Name} = default;");
            }

            return builder.ToString();
        }
    }
}