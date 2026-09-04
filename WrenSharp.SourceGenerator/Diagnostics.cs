using Microsoft.CodeAnalysis;

namespace WrenSharp.Generators;

    internal static class Diagnostics
    {
        private const string Category = "WrenSharp.Generators";

        public static readonly DiagnosticDescriptor ClassMustBePartial = new DiagnosticDescriptor(
            id: "WREN001",
            title: "WrenClass type must be partial",
            messageFormat: "Type '{0}' is annotated with [WrenClass] but is not declared 'partial'. " +
                            "The generator needs to add a Bind() method to this type.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsupportedParameterType = new DiagnosticDescriptor(
            id: "WREN002",
            title: "Unsupported [WrenMethod] parameter type",
            messageFormat: "Parameter '{0}' on method '{1}' has type '{2}', which has no known Wren marshaling. " +
                            "Supported types: bool, double, float, byte, sbyte, short, ushort, int, uint, long, ulong, string.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsupportedReturnType = new DiagnosticDescriptor(
            id: "WREN003",
            title: "Unsupported [WrenMethod] return type",
            messageFormat: "Method '{0}' has return type '{1}', which has no known Wren marshaling. " +
                            "Supported types: void, bool, double, float, byte, sbyte, short, ushort, int, uint, long, ulong, string.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DuplicateWrenSignature = new DiagnosticDescriptor(
            id: "WREN004",
            title: "Duplicate Wren method signature",
            messageFormat: "Type '{0}' has more than one [WrenMethod] member producing the Wren signature '{1}'. " +
                            "Give one of them an explicit name via [WrenMethod(\"name\")].",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor TooManyParameters = new DiagnosticDescriptor(
            id: "WREN005",
            title: "Too many parameters for a Wren method",
            messageFormat: "Method '{0}' has {1} parameters, but Wren methods support a maximum of 16",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }