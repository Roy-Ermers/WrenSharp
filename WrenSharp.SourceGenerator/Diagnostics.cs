using Microsoft.CodeAnalysis;

namespace WrenSharp.SourceGenerator;

    internal static class Diagnostics
    {
        private const string Category = nameof(WrenSharp.SourceGenerator);

        public static readonly DiagnosticDescriptor ClassMustBePartial = new(
            id: "WREN001",
            title: "WrenClass type must be partial",
            messageFormat: "Type '{0}' is annotated with [WrenClass] but is not declared 'partial'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DuplicateWrenSignature = new(
            id: "WREN002",
            title: "Duplicate Wren method signature",
            messageFormat: "Type '{0}' has more than one [WrenMethod] member producing the Wren signature '{1}'. " +
                            "Give one of them an explicit name via [WrenMethod(\"name\")].",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor TooManyParameters = new(
            id: "WREN003",
            title: "Too many parameters for a Wren method",
            messageFormat: "Method '{0}' has {1} parameters, but Wren methods support a maximum of 16",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }