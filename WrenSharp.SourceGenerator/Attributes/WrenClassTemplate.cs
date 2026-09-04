using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace WrenSharp.SourceGenerator.Attributes;

public static partial class Templates
{
    public static SourceText WrenClassAttribute =>
        SourceText.From($$"""
                        using System;
                        using Microsoft.CodeAnalysis;
                        namespace {{Namespace}}
                        {
                            [AttributeUsage(AttributeTargets.Class), Embedded]
                            internal sealed class WrenClassAttribute : Attribute
                            {
                                /// <summary>
                                /// The Wren module the foreign class is declared in (e.g. "main").
                                /// </summary>
                                public string? Module { get; }
                                
                                /// <summary>
                                /// The name of the Wren class. If null, the annotated C# type's name is used.
                                /// </summary>
                                public string? ClassName { get; }
                                
                                public WrenClassAttribute()
                                {
                                    
                                }
                                
                                public WrenClassAttribute(string module)
                                {
                                    Module = module;
                                }  
                                                              
                                public WrenClassAttribute(string module, string className)
                                {
                                    Module = module;
                                    ClassName = className;
                                }
                            }
                        }
                        """, Encoding.UTF8);
}