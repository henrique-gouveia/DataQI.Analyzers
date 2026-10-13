using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DataQI.Analyzers.Test
{
    internal static class TestSymbols
    {
        private const string Prelude = @"
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
namespace System.ComponentModel.DataAnnotations.Schema { public class NotMappedAttribute : Attribute { } }
public enum Color { Red }
";

        public static CSharpCompilation Compile(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(Prelude + source);
            return CSharpCompilation.Create("Test", new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        }

        public static IPropertySymbol Property(string typeName, string attributes = "")
        {
            var compilation = Compile($"public class Holder {{ {attributes} public {typeName} Value {{ get; set; }} }}");
            return compilation.GetTypeByMetadataName("Holder").GetMembers("Value").OfType<IPropertySymbol>().Single();
        }
    }
}
