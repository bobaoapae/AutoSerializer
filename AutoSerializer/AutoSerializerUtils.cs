using System.Linq;
using Microsoft.CodeAnalysis;

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

    public static bool IsList(ITypeSymbol typeSymbol)
    {
        return typeSymbol.AllInterfaces.Any(symbol =>
            symbol.Name == "ICollection" || symbol.Name == "IReadOnlyCollection`1");
    }
}