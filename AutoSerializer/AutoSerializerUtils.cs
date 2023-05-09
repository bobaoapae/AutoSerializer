using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoSerializer;

public static class AutoSerializerUtils
{
    public static bool NeedUseAutoSerializeOrDeserialize(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            if (namedTypeSymbol.GetAttributes()
                    .Any(symbol => symbol.AttributeClass?.Name is "AutoSerializeAttribute") || namedTypeSymbol
                    .GetAttributes().Any(symbol => symbol.AttributeClass?.Name is "AutoDeserializeAttribute"))
            {
                return true;
            }

            if (IsList(namedTypeSymbol))
            {
                return NeedUseAutoSerializeOrDeserialize(namedTypeSymbol.TypeArguments[0]);
            }
        }
        else if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            return NeedUseAutoSerializeOrDeserialize(arrayTypeSymbol.ElementType);
        }

        return false;
    }
    
    public static bool IsBothAutoSerializeAndDeserialize(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol) 
            return false;
        return namedTypeSymbol.GetAttributes()
            .Any(symbol => symbol.AttributeClass?.Name is "AutoSerializeAttribute") && namedTypeSymbol
            .GetAttributes().Any(symbol => symbol.AttributeClass?.Name is "AutoDeserializeAttribute");
    }
    
    public static bool CheckClassIsPartial(INamedTypeSymbol namedTypeSymbol)
    {
        foreach (var declaringSyntaxReference in namedTypeSymbol.DeclaringSyntaxReferences)
        {
            if (CheckClassIsPartial((ClassDeclarationSyntax) declaringSyntaxReference.GetSyntax()))
                return true;
        }

        return false;
    }

    public static bool CheckClassIsPublic(INamedTypeSymbol namedTypeSymbol)
    {
        foreach (var declaringSyntaxReference in namedTypeSymbol.DeclaringSyntaxReferences)
        {
            if (CheckClassIsPublic((ClassDeclarationSyntax) declaringSyntaxReference.GetSyntax()))
                return true;
        }

        return false;
    }

    public static bool CheckClassIsPublic(ClassDeclarationSyntax classDeclarationSyntax)
    {
        return classDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword);
    }

    public static bool CheckClassIsPartial(ClassDeclarationSyntax classDeclarationSyntax)
    {
        return classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    public static bool IsList(ITypeSymbol typeSymbol)
    {
        return typeSymbol.AllInterfaces.Any(symbol => symbol.Name == "ICollection" || symbol.Name == "IReadOnlyCollection`1");
    }

    public static bool IsMemory(ITypeSymbol fieldSymbolType)
    {
        return fieldSymbolType.Name == "Memory`1";
    }

    public static string GetResource(Assembly assembly, SourceProductionContext context, string resourceName)
    {
        using (var resourceStream = assembly.GetManifestResourceStream($"AutoSerializer.Resources.{resourceName}.g"))
        {
            if (resourceStream != null)
                return new StreamReader(resourceStream).ReadToEnd();

            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "ADG0001",
                    "Invalid Resource",
                    $"Cannot find {resourceName}.g resource",
                    "",
                    DiagnosticSeverity.Error,
                    true),
                null));
            return "";
        }
    }
}