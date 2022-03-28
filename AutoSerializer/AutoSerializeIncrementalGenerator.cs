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
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarationsServer = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGenerationSerialize(ctx))
            .Where(static m => m is not null);

        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClassesServer
            = context.CompilationProvider.Combine(classDeclarationsServer.Collect());

        context.RegisterSourceOutput(compilationAndClassesServer,
            static (spc, source) => AutoSerializeGenerator.Generate(source.Item1, source.Item2, spc));

        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarationsClient = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGenerationDeserialize(ctx))
            .Where(static m => m is not null);

        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClassesClient
            = context.CompilationProvider.Combine(classDeclarationsClient.Collect());

        context.RegisterSourceOutput(compilationAndClassesClient,
            static (spc, source) => AutoDeserializeGenerator.Generate(source.Item1, source.Item2, spc));
    }

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        if (node is ClassDeclarationSyntax classDeclarationSyntax && AutoSerializerUtils.CheckClassIsPublic(classDeclarationSyntax) && AutoSerializerUtils.CheckClassIsPartial(classDeclarationSyntax))
        {
            return true;
        }

        return false;
    }

    private static ClassDeclarationSyntax GetSemanticTargetForGenerationSerialize(GeneratorSyntaxContext context)
    {
        var attributeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("AutoSerializer.Definitions.AutoSerializeAttribute")!;

        var classDeclarationSyntax = (ClassDeclarationSyntax) context.Node;

        var model = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

        if (model is INamedTypeSymbol namedTypeSymbol)
        {
            if (AutoSerializerUtils.CheckClassIsPublic(namedTypeSymbol) && AutoSerializerUtils.CheckClassIsPartial(namedTypeSymbol) && namedTypeSymbol.GetAttributes().Any(symbol => symbol.AttributeClass?.Name == attributeSymbol.Name))
            {
                return classDeclarationSyntax;
            }
        }

        return null;
    }

    private static ClassDeclarationSyntax GetSemanticTargetForGenerationDeserialize(GeneratorSyntaxContext context)
    {
        var attributeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("AutoSerializer.Definitions.AutoDeserializeAttribute")!;

        var classDeclarationSyntax = (ClassDeclarationSyntax) context.Node;

        var model = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

        if (model is INamedTypeSymbol namedTypeSymbol)
        {
            if (AutoSerializerUtils.CheckClassIsPublic(namedTypeSymbol) && AutoSerializerUtils.CheckClassIsPartial(namedTypeSymbol) && namedTypeSymbol.GetAttributes().Any(symbol => symbol.AttributeClass?.Name == attributeSymbol.Name))
            {
                return classDeclarationSyntax;
            }
        }

        return null;
    }
}