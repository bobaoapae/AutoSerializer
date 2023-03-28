using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        // var compilationAndClassesServer = context.CompilationProvider.Combine(classDeclarationsServer.Where(static m => IsNamedTargetForGenerationSerialize(m)).Collect());
        //
        // context.RegisterSourceOutput(compilationAndClassesServer,
        //     static (spc, source) => AutoSerializeGenerator.Generate(source.Item1, source.Item2, spc));

        var compilationAndClassesClient = context.CompilationProvider.Combine(classDeclarationsServer
            .Where(static m => IsNamedTargetForGenerationDeserialize(m)).Collect());

        context.RegisterSourceOutput(compilationAndClassesClient,
            static (spc, source) => AutoDeserializeUnsafeStructGenerator.Generate(source.Item1, source.Item2, spc));
    }

    //check if struct partial and has AutoDeserialize or AutoSerialize attribute
    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        if (node is not StructDeclarationSyntax structDeclarationSyntax)
            return false;

        if (!structDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
            return false;
        if (structDeclarationSyntax.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToString() == "AutoSerialize" || y.Name.ToString() == "AutoDeserialize" )))
            return true;

        return false;
    }

    private static INamedTypeSymbol GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var structDeclarationSyntax = (StructDeclarationSyntax)context.Node;

        var model = context.SemanticModel.GetDeclaredSymbol(structDeclarationSyntax);

        return model;
    }

    private static bool IsNamedTargetForGenerationSerialize(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetAttributes().Any(x => x.AttributeClass!.Name == "AutoSerializeAttribute");
    }

    private static bool IsNamedTargetForGenerationDeserialize(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetAttributes().Any(x => x.AttributeClass!.Name == "AutoDeserializeAttribute");
    }
}