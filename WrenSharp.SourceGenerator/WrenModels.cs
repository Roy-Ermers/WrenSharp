using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace WrenSharp.SourceGenerator.Models;

/// <summary>
/// Represents Wren data types, used for indicating the type of a value in a variable or slot.
/// </summary>
public enum WrenType
{
    /// <summary>
    /// A boolean value.<para/>
    /// <b>C value:</b> <c>WREN_TYPE_BOOL</c>
    /// </summary>
    Bool,

    /// <summary>
    /// A number value.<para/>
    /// <b>C value:</b> <c>WREN_TYPE_NUM</c>
    /// </summary>
    Number,

    /// <summary>
    /// A foreign value.<para/>
    /// <b>C value:</b> <c>WREN_TYPE_FOREIGN</c>
    /// </summary>
    Foreign,

    /// <summary>
    /// A list value.<para/>
    /// <b>C value:</b> <c>WREN_TYPE_LIST</c>
    /// </summary>
    List,

    /// <summary>
    /// A map value.<para/>
    /// <b>C value:</b> <c>WREN_TYPE_MAP</c>
    /// </summary>
    Map,

    /// <summary>
    /// A null value.<para/>
    /// <b>C value:</b> <c>WREN_TYPE_NULL</c>
    /// </summary>
    Null,

    /// <summary>
    /// A string value.<para/>
    /// <b>C value:</b> <c>WREN_TYPE_STRING</c>
    /// </summary>
    String,

    /// <summary>
    /// A unknown value. Receiving this type indicates an error.<para/>
    /// <b>C value:</b> <c>WREN_TYPE_UNKNOWN</c>
    /// </summary>
    Unknown
}

public static class WrenTypeExtensions
{
    public static WrenType FromCsharpType(this WrenType _, string csharpType)
    {
        return csharpType switch
        {
            "bool" => WrenType.Bool,
            "string" =>  WrenType.String,
            "double" or "float" or "int" => WrenType.Number,
            _ => WrenType.Unknown
        };
    }    
    
    public static WrenType FromCsharpType(string csharpType)
    {
        return csharpType switch
        {
            "bool" => WrenType.Bool,
            "string" =>  WrenType.String,
            "double" or "float" or "int" => WrenType.Number,
            _ => WrenType.Unknown
        };
    }

    public static string ToWrenGetArgMethod(string csharpType, int slot, string ctx = "ctx")
    {
        var method = csharpType switch
        {
            "bool" => "GetArgBool",
            "string" => "GetArgString",
            "double" => "GetArgDouble",
            "float" => "GetArgFloat",
            "int" => "GetArgInt32",
            _ => throw new NotImplementedException()
        };

        return $"{ctx}.{method}({slot})";
    }
}

public record WrenClassModel(
    string? Name,
    string CsName,
    string Module,
    string? Namespace,
    ImmutableArray<WrenConstructor> Constructors,
    ImmutableArray<WrenMethod> Methods,
    ImmutableArray<WrenProperty> Properties,
    ImmutableArray<Diagnostic> Diagnostics);

public record WrenParameter(
    string Name,
    string Type
);

public record WrenConstructor(
    List<WrenParameter> Parameters
);

public record WrenProperty(
    string Name,
    string Type,
    bool Getter,
    bool Setter
);

public record WrenMethod(
    string Name,
    string CsMethodName,
    bool IsStatic,
    string ReturnType,
    List<WrenParameter> Parameters
)
{
    public string GetSignature()
    {
        var name = char.ToLowerInvariant(Name[0]) + Name.Substring(1);
        
        return name + '(' + string.Join(", ", Enumerable.Repeat("_", Parameters.Count)) + ')';
    }
}
