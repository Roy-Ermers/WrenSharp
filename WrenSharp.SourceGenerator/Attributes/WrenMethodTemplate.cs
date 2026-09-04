using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace WrenSharp.SourceGenerator.Attributes;

public static partial class Templates
{
    public static SourceText WrenMethodAttribute =>
        SourceText.From($$"""
                        using System;
                        using Microsoft.CodeAnalysis;
                        namespace {{Namespace}}
                        {
                        [AttributeUsage(AttributeTargets.Method)]
                        public class WrenMethodAttribute: Attribute
                        {
                            public string? Name { get; }
                            
                            public WrenMethodAttribute(string name)
                            {
                                Name = name;
                            }
                            
                            public WrenMethodAttribute() {}
                        }
                        }
                        """, Encoding.UTF8);
}