using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoSerializer;

[Generator]
public class AutoSerializeIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarationsServer = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx));

        var compilationAndClassesSerialize = context.CompilationProvider.Combine(classDeclarationsServer.Where(static m => IsNamedTargetForGenerationSerialize(m)).Collect());

        context.RegisterSourceOutput(compilationAndClassesSerialize,
            static (spc, source) => AutoSerializeGenerator.Generate(source.Item1, source.Item2, spc));

        var compilationAndClassesDeserialize = context.CompilationProvider.Combine(classDeclarationsServer.Where(static m => IsNamedTargetForGenerationDeserialize(m)).Collect());

        context.RegisterSourceOutput(compilationAndClassesDeserialize,
            static (spc, source) => AutoDeserializeGenerator.Generate(source.Item1, source.Item2, spc));
        
        var compilationAndClassesBoth = context.CompilationProvider.Combine(classDeclarationsServer.Where(static m => IsNamedTargetForGenerationSerialize(m) || IsNamedTargetForGenerationDeserialize(m)).Collect());
        
        context.RegisterSourceOutput(compilationAndClassesBoth,
            static (spc, source) => AutoPooledGenerator.Generate(source.Item1, source.Item2, spc));
        
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDeclarationSyntax && AutoSerializerUtils.CheckClassIsPublic(classDeclarationSyntax) && AutoSerializerUtils.CheckClassIsPartial(classDeclarationSyntax);
    }

    private static INamedTypeSymbol GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax) context.Node;

        var model = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

        return (INamedTypeSymbol) model;
    }

    private static bool IsNamedTargetForGenerationSerialize(INamedTypeSymbol namedTypeSymbol)
    {
        return AutoSerializerUtils.CheckClassIsPublic(namedTypeSymbol) && AutoSerializerUtils.CheckClassIsPartial(namedTypeSymbol) && namedTypeSymbol.GetAttributes().Any(symbol => symbol.AttributeClass?.Name == "AutoSerializeAttribute");
    }

    private static bool IsNamedTargetForGenerationDeserialize(INamedTypeSymbol namedTypeSymbol)
    {
        return AutoSerializerUtils.CheckClassIsPublic(namedTypeSymbol) && AutoSerializerUtils.CheckClassIsPartial(namedTypeSymbol) && namedTypeSymbol.GetAttributes().Any(symbol => symbol.AttributeClass?.Name == "AutoDeserializeAttribute");
    }
}