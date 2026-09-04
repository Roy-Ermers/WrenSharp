using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WrenSharp.Generators;

namespace WrenSharp.SourceGenerator;

public sealed class WrenClassGenerator : IIncrementalGenerator
{
    private const string WrenClassAttributeFullName = "WrenSharp.Generators.Attributes.WrenClassAttribute";
    private const string WrenMethodAttributeFullName = "WrenSharp.Generators.Attributes.WrenMethodAttribute";
    private const string WrenPropertyAttributeFullName = "WrenSharp.Generators.Attributes.WrenPropertyAttribute";
    private const string WrenIgnoreAttributeFullName = "WrenSharp.Generators.Attributes.WrenIgnoreAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WrenClassAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => Analyze(ctx))
            .Where(static t => t is not null)!;

        context.RegisterSourceOutput(targets, static (spc, target) => Emit(spc, target!));
    }

    private static TargetTypeInfo Analyze(GeneratorAttributeSyntaxContext ctx)
    {
        var classSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var classSyntax = (ClassDeclarationSyntax)ctx.TargetNode;

        var wrenClassAttr = ctx.Attributes[0];
        var module = wrenClassAttr.ConstructorArguments.Length > 0
            ? wrenClassAttr.ConstructorArguments[0].Value as string
            : "engine";
        var explicitClassName = wrenClassAttr.ConstructorArguments.Length > 1
            ? wrenClassAttr.ConstructorArguments[1].Value as string
            : null;

        var isPartial = classSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);

        var constructors = new List<ConstructorModel>();
        var methods = new List<MethodModel>();
        var properties = new List<PropertyModel>();
        var diagnostics = new List<Diagnostic>();

        // Analyze Constructors
        foreach (var ctor in classSymbol.Constructors)
        {
            if (ctor.IsImplicitlyDeclared || ctor.DeclaredAccessibility != Accessibility.Public)
                continue;

            var paramTypesOk = true;
            var paramModels = new List<ParamModel>();
            foreach (var p in ctor.Parameters)
            {
                var clrType = p.Type.ToDisplayString();
                if (!TypeMarshalMap.TryGet(clrType, out var marshal))
                {
                    diagnostics.Add(Diagnostic.Create(Diagnostics.UnsupportedParameterType,
                        p.Locations.FirstOrDefault(), p.Name, ".ctor", clrType));
                    paramTypesOk = false;
                    break;
                }
                paramModels.Add(new ParamModel(p.Name, clrType, marshal));
            }

            if (!paramTypesOk)
                continue;

            constructors.Add(new ConstructorModel(paramModels));
        }

        foreach (var member in classSymbol.GetMembers())
        {
            switch (member)
            {
                case IMethodSymbol { MethodKind: MethodKind.Ordinary } methodSymbol:
                {
                    var wrenMethodAttr = methodSymbol.GetAttributes()
                        .FirstOrDefault(a => a.AttributeClass?.Name == "WrenMethodAttribute" || 
                                             a.AttributeClass?.Name == "WrenMethod");
                    if (wrenMethodAttr is null)
                        continue;

                    if (methodSymbol.Parameters.Length > 16)
                    {
                        diagnostics.Add(Diagnostic.Create(Diagnostics.TooManyParameters,
                            methodSymbol.Locations.FirstOrDefault(), methodSymbol.Name, methodSymbol.Parameters.Length));
                        continue;
                    }

                    var explicitName = wrenMethodAttr.ConstructorArguments.Length > 0
                        ? wrenMethodAttr.ConstructorArguments[0].Value as string
                        : null;

                    var paramTypesOk = true;
                    var paramModels = new List<ParamModel>();
                    foreach (var p in methodSymbol.Parameters)
                    {
                        var clrType = p.Type.ToDisplayString();
                        if (!TypeMarshalMap.TryGet(clrType, out var marshal))
                        {
                            diagnostics.Add(Diagnostic.Create(Diagnostics.UnsupportedParameterType,
                                p.Locations.FirstOrDefault(), p.Name, methodSymbol.Name, clrType));
                            paramTypesOk = false;
                            continue;
                        }
                        paramModels.Add(new ParamModel(p.Name, clrType, marshal));
                    }

                    var returnClrType = methodSymbol.ReturnType.SpecialType == SpecialType.System_Void
                        ? "void"
                        : methodSymbol.ReturnType.ToDisplayString();
                    MarshalInfo returnMarshal = default;
                    var returnOk = returnClrType == "void" || TypeMarshalMap.TryGet(returnClrType, out returnMarshal);
                    if (!returnOk)
                    {
                        diagnostics.Add(Diagnostic.Create(Diagnostics.UnsupportedReturnType,
                            methodSymbol.Locations.FirstOrDefault(), methodSymbol.Name, returnClrType));
                    }

                    if (!paramTypesOk || !returnOk)
                        continue;

                    var wrenName = explicitName ?? ToWrenMemberName(methodSymbol.Name);
                    var signature = BuildMethodSignature(wrenName, paramModels.Count);

                    methods.Add(new MethodModel(
                        csharpName: methodSymbol.Name,
                        isStatic: methodSymbol.IsStatic,
                        signature: signature,
                        wrenName: wrenName,
                        returnClrType: returnClrType,
                        returnMarshal: returnMarshal,
                        parameters: paramModels));
                    break;
                }
                case IPropertySymbol propSymbol:
                {
                    var ignored = propSymbol.GetAttributes()
                        .Any(a => a.AttributeClass?.Name == "WrenIgnoreAttribute" || 
                                  a.AttributeClass?.Name == "WrenIgnore");
                    if (ignored)
                        continue;

                    var wrenPropAttr = propSymbol.GetAttributes()
                        .FirstOrDefault(a => a.AttributeClass?.Name == "WrenPropertyAttribute" || 
                                             a.AttributeClass?.Name == "WrenProperty");
                    if (wrenPropAttr is null)
                        continue;

                    var explicitName = wrenPropAttr.ConstructorArguments.Length > 0
                        ? wrenPropAttr.ConstructorArguments[0].Value as string
                        : null;

                    var clrType = propSymbol.Type.ToDisplayString();
                    if (!TypeMarshalMap.TryGet(clrType, out var marshal))
                    {
                        diagnostics.Add(Diagnostic.Create(Diagnostics.UnsupportedParameterType,
                            propSymbol.Locations.FirstOrDefault(), propSymbol.Name, propSymbol.Name, clrType));
                        continue;
                    }

                    var wrenName = explicitName ?? ToWrenMemberName(propSymbol.Name);
                    properties.Add(new PropertyModel(
                        csharpName: propSymbol.Name,
                        wrenName: wrenName,
                        clrType: clrType,
                        marshal: marshal,
                        hasGetter: propSymbol.GetMethod is not null,
                        hasSetter: propSymbol.SetMethod is not null));
                    break;
                }
            }
        }

        if (!isPartial)
        {
            diagnostics.Add(Diagnostic.Create(Diagnostics.ClassMustBePartial,
                classSymbol.Locations.FirstOrDefault(), classSymbol.Name));
        }

        return new TargetTypeInfo(
            namespaceName: classSymbol.ContainingNamespace.IsGlobalNamespace ? null : classSymbol.ContainingNamespace.ToDisplayString(),
            typeName: classSymbol.Name,
            module: module,
            wrenClassName: string.IsNullOrEmpty(explicitClassName) ? classSymbol.Name : explicitClassName,
            isPartial: isPartial,
            constructors: constructors,
            methods: methods,
            properties: properties,
            diagnostics: diagnostics);
    }

    private static string ToWrenMemberName(string csharpName)
    {
        if (string.IsNullOrEmpty(csharpName))
            return csharpName;
        return char.ToLowerInvariant(csharpName[0]) + csharpName.Substring(1);
    }

    private static string BuildMethodSignature(string wrenName, int paramCount)
    {
        if (paramCount == 0)
            return wrenName + "()";
        return wrenName + "(" + string.Join(",", Enumerable.Repeat("_", paramCount)) + ")";
    }

    private static void Emit(SourceProductionContext spc, TargetTypeInfo target)
    {
        foreach (var diagnostic in target.Diagnostics)
            spc.ReportDiagnostic(diagnostic);

        if (target.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            return;

        var source = GeneratedSource(target);
        var hintName = (target.NamespaceName is null ? target.TypeName : $"{target.NamespaceName}.{target.TypeName}")
                     + ".WrenBindings.g.cs";
        spc.AddSource(hintName, source);
    }

    private static string GeneratedSource(TargetTypeInfo target)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by WrenSharp.Generators from [WrenClass] on " + target.TypeName + ".");
        sb.AppendLine("#nullable disable");
        sb.AppendLine("using WrenSharp;");
        sb.AppendLine();

        var hasNamespace = target.NamespaceName is not null;
        var indent = "";
        if (hasNamespace)
        {
            sb.Append("namespace ").Append(target.NamespaceName).AppendLine();
            sb.AppendLine("{");
            indent = "    ";
        }

        sb.Append(indent).Append("partial class ").Append(target.TypeName).AppendLine();
        sb.Append(indent).AppendLine("{");

        // --- WrenSource Property ---
        sb.Append(indent).AppendLine("    /// <summary>");
        sb.Append(indent).AppendLine($"    /// The generated Wren foreign class script for \"{target.WrenClassName}\".");
        sb.Append(indent).AppendLine("    /// </summary>");
        sb.Append(indent).AppendLine("    public static string WrenSource => @\"");
        sb.Append(indent).AppendLine($"    foreign class {target.WrenClassName} {{");

        foreach (var ctor in target.Constructors)
        {
            var paramNames = string.Join(", ", ctor.Parameters.Select(p => p.Name));
            sb.Append(indent).Append("        construct new(").Append(paramNames).AppendLine(") {}");
        }
        if (target.Constructors.Count == 0)
        {
            sb.Append(indent).AppendLine("        construct new() { }");
        }

        foreach (var prop in target.Properties)
        {
            if (prop.HasGetter)
                sb.Append(indent).AppendLine($"        foreign {prop.WrenName}");
            if (prop.HasSetter)
                sb.Append(indent).AppendLine($"        foreign {prop.WrenName}=(value)");
        }

        foreach (var method in target.Methods)
        {
            var paramNames = string.Join(", ", method.Parameters.Select(p => p.Name));
            sb.Append(indent).AppendLine($"        foreign {method.WrenName}({paramNames})");
        }

        sb.Append(indent).AppendLine("    }\";");
        sb.AppendLine();

        // --- Bind Method ---
        sb.Append(indent).AppendLine("    /// <summary>");
        sb.Append(indent).AppendLine($"    /// Registers the \"{target.WrenClassName}\" foreign class in module \"{target.Module}\",");
        sb.Append(indent).AppendLine("    /// generated from [WrenMethod]/[WrenProperty] members of this type.");
        sb.Append(indent).AppendLine("    /// </summary>");
        sb.Append(indent).AppendLine("    public static void Bind(WrenSharpVM vm)");
        sb.Append(indent).AppendLine("    {");
        sb.Append(indent).Append("        vm.Foreign(").Append('"').Append(target.Module).Append('"')
          .Append(", ").Append('"').Append(target.WrenClassName).Append('"').AppendLine(")");

        // Allocate block using WrenCallContext
        sb.Append(indent).AppendLine("            .Allocate((WrenCallContext ctx) =>");
        sb.Append(indent).AppendLine("            {");
        sb.Append(indent).AppendLine("                var args = new System.Collections.Generic.List<WrenType>();");
        sb.Append(indent).AppendLine("                for (int i = 1; i < 16; i++)");
        sb.Append(indent).AppendLine("                {");
        sb.Append(indent).AppendLine("                    Console.WriteLine(ctx.GetArgType(i));");
        sb.Append(indent).AppendLine("                    args.Add(ctx.GetArgType(i));");
        sb.Append(indent).AppendLine("                }");
        sb.Append(indent).AppendLine();
        sb.Append(indent).AppendLine("                " + target.TypeName + " instance = args.Count switch");
        sb.Append(indent).AppendLine("                {");

        if (target.Constructors.Count == 0)
        {
            sb.Append(indent).Append("                    0 => new ").Append(target.TypeName).AppendLine("(),");
        }
        else
        {
            foreach (var ctor in target.Constructors)
            {
                sb.Append(indent).Append("                    ").Append(ctor.Parameters.Count).Append(" => new ").Append(target.TypeName).Append('(');
                var args = new List<string>();
                for (var i = 0; i < ctor.Parameters.Count; i++)
                {
                    var p = ctor.Parameters[i];
                    args.Add($"ctx.{p.Marshal.GetArgMethod}({i})");
                }
                sb.Append(string.Join(", ", args)).AppendLine("),");
            }
        }

        sb.Append(indent).AppendLine("                    _ => throw new System.InvalidOperationException(");
        sb.Append(indent).AppendLine("                        $\"No matching constructor found for argument count {args}.\")");
        sb.Append(indent).AppendLine("                };");
        sb.AppendLine();
        sb.Append(indent).AppendLine("                ctx.ReturnSharedData(instance);");
        sb.Append(indent).AppendLine("            })");

        // Methods & Properties via .Instance(...)
        foreach (var method in target.Methods)
        {
            sb.Append(indent).Append("            .Instance(\"").Append(method.Signature).AppendLine("\", ctx =>");
            sb.Append(indent).AppendLine("            {");
            sb.Append(indent).Append("                var self = ctx.GetReceiverSharedData<").Append(target.TypeName).AppendLine(">();");

            for (var i = 0; i < method.Parameters.Count; i++)
            {
                var p = method.Parameters[i];
                sb.Append(indent).Append("                var arg").Append(i)
                  .Append(" = ctx.").Append(p.Marshal.GetArgMethod).Append('(').Append(i).AppendLine(");");
            }

            var args = string.Join(", ", Enumerable.Range(0, method.Parameters.Count).Select(i => "arg" + i));
            var callTarget = method.IsStatic ? method.CSharpName : "self." + method.CSharpName;

            if (method.ReturnClrType == "void")
            {
                sb.Append(indent).Append("                ").Append(callTarget).Append('(').Append(args).AppendLine(");");
            }
            else
            {
                sb.Append(indent).Append("                ctx.Return(").Append(callTarget).Append('(').Append(args).AppendLine("));");
            }

            sb.Append(indent).AppendLine("            })");
        }

        foreach (var prop in target.Properties)
        {
            if (prop.HasGetter)
            {
                sb.Append(indent).Append("            .Instance(\"").Append(prop.WrenName).AppendLine("\", ctx =>");
                sb.Append(indent).AppendLine("            {");
                sb.Append(indent).Append("                var self = ctx.GetReceiverSharedData<").Append(target.TypeName).AppendLine(">();");
                sb.Append(indent).Append("                ctx.Return(").Append(prop.Marshal.ReturnCast).Append("self.").Append(prop.CSharpName).AppendLine(");");
                sb.Append(indent).AppendLine("            })");
            }

            if (prop.HasSetter)
            {
                sb.Append(indent).Append("            .Instance(\"").Append(prop.WrenName).AppendLine("=(_)\", ctx =>");
                sb.Append(indent).AppendLine("            {");
                sb.Append(indent).Append("                var self = ctx.GetReceiverSharedData<").Append(target.TypeName).AppendLine(">();");
                sb.Append(indent).Append("                self.").Append(prop.CSharpName).Append(" = ctx.").Append(prop.Marshal.GetArgMethod).AppendLine("(0);");
                sb.Append(indent).AppendLine("            })");
            }
        }

        sb.Append(indent).AppendLine("            ;");
        sb.Append(indent).AppendLine("    }");
        sb.Append(indent).AppendLine("}");

        if (hasNamespace)
            sb.AppendLine("}");

        return sb.ToString();
    }

    private sealed class TargetTypeInfo
    {
        public string NamespaceName { get; }
        public string TypeName { get; }
        public string Module { get; }
        public string WrenClassName { get; }
        public bool IsPartial { get; }
        public List<ConstructorModel> Constructors { get; }
        public List<MethodModel> Methods { get; }
        public List<PropertyModel> Properties { get; }
        public List<Diagnostic> Diagnostics { get; }

        public TargetTypeInfo(string namespaceName, string typeName, string module, string wrenClassName,
            bool isPartial, List<ConstructorModel> constructors, List<MethodModel> methods, List<PropertyModel> properties, List<Diagnostic> diagnostics)
        {
            NamespaceName = namespaceName;
            TypeName = typeName;
            Module = module;
            WrenClassName = wrenClassName;
            IsPartial = isPartial;
            Constructors = constructors;
            Methods = methods;
            Properties = properties;
            Diagnostics = diagnostics;
        }
    }

    private sealed class ConstructorModel
    {
        public List<ParamModel> Parameters { get; }

        public ConstructorModel(List<ParamModel> parameters)
        {
            Parameters = parameters;
        }
    }

    private sealed class MethodModel
    {
        public string CSharpName { get; }
        public bool IsStatic { get; }
        public string Signature { get; }
        public string WrenName { get; }
        public string ReturnClrType { get; }
        public MarshalInfo ReturnMarshal { get; }
        public List<ParamModel> Parameters { get; }

        public MethodModel(string csharpName, bool isStatic, string signature, string wrenName, string returnClrType,
            MarshalInfo returnMarshal, List<ParamModel> parameters)
        {
            CSharpName = csharpName;
            IsStatic = isStatic;
            Signature = signature;
            WrenName = wrenName;
            ReturnClrType = returnClrType;
            ReturnMarshal = returnMarshal;
            Parameters = parameters;
        }
    }

    private readonly struct ParamModel
    {
        public readonly string Name;
        public readonly string ClrType;
        public readonly MarshalInfo Marshal;

        public ParamModel(string name, string clrType, MarshalInfo marshal)
        {
            Name = name;
            ClrType = clrType;
            Marshal = marshal;
        }
    }

    private sealed class PropertyModel
    {
        public string CSharpName { get; }
        public string WrenName { get; }
        public string ClrType { get; }
        public MarshalInfo Marshal { get; }
        public bool HasGetter { get; }
        public bool HasSetter { get; }

        public PropertyModel(string csharpName, string wrenName, string clrType, MarshalInfo marshal,
            bool hasGetter, bool hasSetter)
        {
            CSharpName = csharpName;
            WrenName = wrenName;
            ClrType = clrType;
            Marshal = marshal;
            HasGetter = hasGetter;
            HasSetter = hasSetter;
        }
    }
}