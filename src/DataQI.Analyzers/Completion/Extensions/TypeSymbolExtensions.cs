using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace DataQI.Analyzers.Completion.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class TypeSymbolExtensions
    {
        private static readonly string[] NumericLikeTypeNames =
        {
            "Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64",
            "Single", "Double", "Decimal", "DateTime", "DateTimeOffset", "TimeSpan", "Guid"
        };

        public static IReadOnlyList<string> GetSuggestedOperators(this ITypeSymbol type)
        {
            var underlyingType = type;
            var isNullableOrReference = type.IsReferenceType;

            var namedType = type as INamedTypeSymbol;
            if (namedType != null && namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            {
                underlyingType = namedType.TypeArguments[0];
                isNullableOrReference = true;
            }

            var operators = new List<string>();

            if (underlyingType.SpecialType == SpecialType.System_String)
            {
                operators.AddRange(new[] { "Not", "StartingWith", "EndingWith", "Containing", "Like", "In", "NotIn", "Null", "NotNull" });
            }
            else if (underlyingType.SpecialType == SpecialType.System_Boolean)
            {
                operators.Add("Not");
            }
            else if (underlyingType.TypeKind == TypeKind.Enum || NumericLikeTypeNames.Contains(underlyingType.Name))
            {
                operators.AddRange(new[] { "Not", "GreaterThan", "GreaterThanEqual", "LessThan", "LessThanEqual", "Between", "NotBetween", "In", "NotIn" });
                if (isNullableOrReference)
                    operators.AddRange(new[] { "Null", "NotNull" });
            }
            else
            {
                operators.Add("Not");
                if (isNullableOrReference)
                    operators.AddRange(new[] { "Null", "NotNull" });
            }

            return operators;
        }
    }
}
