using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using WrenSharp.Generators;
using WrenSharp.SourceGenerator.Attributes;
using WrenSharp.SourceGenerator.Models;

namespace WrenSharp.SourceGenerator;

public partial class WrenInterop : IIncrementalGenerator
{
    private static WrenClassModel Analyze(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            throw new Exception("WrenClass attribute needs to be attached to class.");
        }
        
        var diagnostics = new List<Diagnostic>();
        
        // get first WrenClass attribute
        var attribute = classSymbol.GetAttributes()
            .First(a => a.AttributeClass?.Name is "WrenClassAttribute" or "WrenClass");

        var module = attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as string : "engine";
        var className = attribute.ConstructorArguments.Length > 1 ? attribute.ConstructorArguments[1].Value as string : null;
        var csharpName = classSymbol.Name;

        var constructors = GetConstructors(classSymbol, diagnostics);
        var (properties, methods) = GetMembers(classSymbol, diagnostics);

        string? ns = null;

        if (!classSymbol.ContainingNamespace.IsGlobalNamespace)
            ns = classSymbol.ContainingNamespace.ToDisplayString();

        return new WrenClassModel(
            className, 
            csharpName,
            module,
            ns,
            [.. constructors],
            methods,
            properties,
            [..diagnostics]
        );
    }

    private static ImmutableArray<WrenConstructor> GetConstructors(INamedTypeSymbol classSymbol,
        List<Diagnostic> _)
    {
        var result = new List<WrenConstructor>();
        foreach (var ctor in classSymbol.Constructors)
        {
            if (ctor.IsImplicitlyDeclared || ctor.DeclaredAccessibility != Accessibility.Public)
                continue;
            
            var parameters = ParseParameters(ctor.Parameters);
    
            result.Add(new WrenConstructor(parameters));
        }

        return [..result];
    }
    
    
    private static (ImmutableArray<WrenProperty> properties, ImmutableArray<WrenMethod> methods) GetMembers(
        INamedTypeSymbol classSymbol, List<Diagnostic> diagnostics)
    {
        var properties = new List<WrenProperty>();
        var methods = new List<WrenMethod>();
        foreach (var member in classSymbol.GetMembers())
        {
            switch (member)
            {
                case IMethodSymbol { MethodKind: MethodKind.Ordinary } methodSymbol:
                {
                    var method = ParseMethod(methodSymbol, diagnostics);
                    if(method is null) continue;
                    
                    methods.Add(method);
                    break;
                }
                case IPropertySymbol propertySymbol:
                {
                    var property = ParseProperty(propertySymbol, diagnostics);
                    if(property is null) continue;
                    
                    properties.Add(property);
                    break;
                }
            }
            
        }

        return (
            properties: [..properties],
            methods: [..methods]
    );  
    }

    private static WrenProperty? ParseProperty(IPropertySymbol symbol, List<Diagnostic> diagnostics)
    {
        var attribute = symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name is "WrenMethodAttribute" or "WrenMethod");
        
        if (attribute is null) 
            return null;
        
        var name = attribute.ConstructorArguments.Length > 0 ? 
            attribute.ConstructorArguments[0].Value! as string : symbol.Name;
        var type = symbol.Type.ToDisplayString();

        var getter = symbol.GetMethod is not null;
        var setter = symbol.SetMethod is not null;

        return new WrenProperty(name, type, getter, setter);
    }

    private static WrenMethod? ParseMethod(IMethodSymbol symbol, List<Diagnostic> diagnostics)
    {
        var attribute = symbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name is "WrenMethodAttribute" or "WrenMethod");
        
        if (attribute is null) 
            return null;
        
        var name = attribute.ConstructorArguments.Length > 0 ? 
            attribute.ConstructorArguments[0].Value! as string : symbol.Name;
        
        var returnType = symbol.ReturnType.SpecialType == SpecialType.System_Void
            ? "void"
            : symbol.ReturnType.ToDisplayString();
        
        if (symbol.Parameters.Length > 16)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.TooManyParameters,
                symbol.Locations.FirstOrDefault(), symbol.Name, symbol.Parameters.Length));
            return null;
        }

        var parameters = ParseParameters(symbol.Parameters);

        return new WrenMethod(name, symbol.Name, symbol.IsStatic, returnType, [.. parameters]);
    }

    private static List<WrenParameter> ParseParameters(ImmutableArray<IParameterSymbol> parameters)
    {
        var result = new List<WrenParameter>();
        foreach (var parameter in parameters)
        {
            var csharpType = parameter.Type.ToDisplayString();
            result.Add(new WrenParameter(parameter.Name, csharpType));    
        }

        return result;
    }
}