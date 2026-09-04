using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace WrenSharp.SourceGenerator.Attributes;

public static partial class Templates
{
    public static SourceText WrenPropertyAttribute =>
        SourceText.From($$"""
                        using System;
                        using Microsoft.CodeAnalysis;
                        namespace {{Namespace}}
                        {
                        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
                        public class WrenPropertyAttribute: Attribute
                        {
                            public string? Name { get; }
                            
                            public WrenPropertyAttribute(string name)
                            {
                                Name = name;
                            }
                            
                            public WrenPropertyAttribute() {}
                        }
                        }
                        """, Encoding.UTF8);
}