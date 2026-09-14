using System.Collections.Immutable;
using WrenSharp.SourceGenerator.helpers;
using WrenSharp.SourceGenerator.Models;

namespace WrenSharp.SourceGenerator;

public class WrenCodeGenerator
{
    public static string Generate(WrenClassModel model)
    {
        IndentedStringBuilder sb = new IndentedStringBuilder();
        Generate(sb, model);
        return sb.ToString();
    }
    
    public static void Generate(IndentedStringBuilder sb, WrenClassModel model)
    {
        sb.Append("foreign class ").Append(model.Name ?? WrenHelpers.ConvertNameToWrenStyle(model.CsName)).AppendLine(" {");
        using (sb.Indented())
        {
            GenerateConstructors(sb, model.Constructors);
            GenerateMethods(sb, model.Methods);
            GenerateProperties(sb, model.Properties);
        }

        sb.AppendLine("}");
    }

    private static void GenerateConstructors(IndentedStringBuilder sb, ImmutableArray<WrenConstructor> constructors)
    {
        foreach (var constructor in constructors)
        {
            sb.Append("construct new(");
            sb.Append(string.Join(", ",  constructor.Parameters.Select(x => x.Name)));
            sb.AppendLine(") {}");
        }
    }

    private static void GenerateMethods(IndentedStringBuilder sb, ImmutableArray<WrenMethod> methods)
    {
        foreach (var signature in methods.GroupBy(m => (m.Name, m.IsStatic, m.Parameters.Count)))
        {
            var (name, isStatic, parameters) = signature.Key;

            sb.Append("foreign ");
            if(isStatic)
                sb.Append("static ");
            
            sb.Append(WrenHelpers.ConvertNameToWrenStyle(name)).Append("(");
            sb.Append(string.Join(", ", signature.First().Parameters.Select(p => p.Name)));
            sb.AppendLine(")");
        }
    }

    private static void GenerateProperties(IndentedStringBuilder sb, ImmutableArray<WrenProperty> properties)
    {
        foreach (var property in properties)
        {
            if(property.Getter)
                sb.Append("foreign ").AppendLine(WrenHelpers.ConvertNameToWrenStyle(property.Name));
            
            if(property.Setter)
                sb.Append("foreign ").Append(WrenHelpers.ConvertNameToWrenStyle(property.Name)).AppendLine("=(value)");
        }
    }
}