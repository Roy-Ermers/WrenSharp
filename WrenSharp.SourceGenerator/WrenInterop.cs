using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WrenSharp.SourceGenerator.Attributes;

namespace WrenSharp.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public partial class WrenInterop : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static postInitializationContext =>
        {
            postInitializationContext.AddEmbeddedAttributeDefinition();
            postInitializationContext.AddSource("WrenClass", Templates.WrenClassAttribute);
            postInitializationContext.AddSource("WrenProperty", Templates.WrenPropertyAttribute);
            postInitializationContext.AddSource("WrenMethod", Templates.WrenMethodAttribute);
        });

        var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: $"{Templates.Namespace}.WrenClassAttribute",
            predicate: static (node, cancellationToken) => node is ClassDeclarationSyntax,
            transform: static (ctx, _) => Analyze(ctx)
        );
        
        context.RegisterSourceOutput(pipeline, static(spc, target) => EmitClass(spc, target!));
        var allClasses = pipeline.Collect();

        context.RegisterSourceOutput(allClasses, static (spc, target) =>
        {
            spc.AddSource("Binding.WrenInterop.g.cs", Templates.BindingClass(target));
        });
    }

}