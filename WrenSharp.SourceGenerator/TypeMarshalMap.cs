namespace WrenSharp.Generators;

    /// <summary>
    /// Describes how a single supported CLR type is marshaled to/from the Wren API stack
    /// via <c>WrenCallContext</c>.
    /// </summary>
    internal readonly struct MarshalInfo
    {
        /// <summary>The WrenCallContext.GetArg* method name used to read an argument of this type.</summary>
        public readonly string GetArgMethod;

        /// <summary>
        /// If non-null, the C# expression fragment used to cast a Return() argument down to a
        /// type WrenCallContext.Return has an overload for (only `double` exists for numerics).
        /// </summary>
        public readonly string ReturnCast;

        public MarshalInfo(string getArgMethod, string returnCast)
        {
            GetArgMethod = getArgMethod;
            ReturnCast = returnCast;
        }
    }

    /// <summary>
    /// Maps supported fully-qualified CLR type names to their WrenCallContext marshaling info.
    /// This mirrors the argument/return surface actually exposed by WrenCallContext (see
    /// WrenSharp.Core.Shared's WrenCallContext.cs) - extend this table if that surface grows.
    /// </summary>
    internal static class TypeMarshalMap
    {
        public static readonly Dictionary<string, MarshalInfo> Map = new Dictionary<string, MarshalInfo>
        {
            // Types with a native Return(T) overload: no cast needed.
            ["bool"] = new MarshalInfo("GetArgBool", null),
            ["double"] = new MarshalInfo("GetArgDouble", null),
            ["string"] = new MarshalInfo("GetArgString", null),

            // Numeric types: WrenCallContext only exposes Return(double), so these need
            // an explicit cast on the way out. Reading uses the dedicated GetArgIntNN accessor.
            ["float"] = new MarshalInfo("GetArgFloat", "(double)"),
            ["byte"] = new MarshalInfo("GetArgInt8", "(double)"),
            ["sbyte"] = new MarshalInfo("GetArgUInt8", "(double)"), // NB: names swapped in WrenCallContext itself
            ["short"] = new MarshalInfo("GetArgInt16", "(double)"),
            ["ushort"] = new MarshalInfo("GetArgUInt16", "(double)"),
            ["int"] = new MarshalInfo("GetArgInt32", "(double)"),
            ["uint"] = new MarshalInfo("GetArgUInt32", "(double)"),
            ["long"] = new MarshalInfo("GetArgInt64", "(double)"),
            ["ulong"] = new MarshalInfo("GetArgUInt64", "(double)"),
        };

        public static bool TryGet(string clrTypeName, out MarshalInfo info) => Map.TryGetValue(clrTypeName, out info);
    }
