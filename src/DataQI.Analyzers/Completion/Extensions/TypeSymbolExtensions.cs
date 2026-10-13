using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace DataQI.Analyzers.Completion.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class TypeSymbolExtensions
    {
        private static readonly string[] ScalarStructNames = { "DateTimeOffset", "TimeSpan", "Guid" };

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

        public static bool IsScalar(this ITypeSymbol type)
        {
            var underlying = Unwrap(type, out _);

            if (underlying.TypeKind == TypeKind.Enum)
                return true;

            var array = underlying as IArrayTypeSymbol;
            if (array != null)
                return array.Rank == 1 && array.ElementType.SpecialType == SpecialType.System_Byte;

            switch (underlying.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                case SpecialType.System_DateTime:
                    return true;
            }

            return ScalarStructNames.Contains(underlying.Name)
                && underlying.ContainingNamespace?.ToDisplayString() == "System";
        }

        private static ITypeSymbol Unwrap(ITypeSymbol type, out bool isNullable)
        {
            isNullable = type.IsReferenceType;

            var named = type as INamedTypeSymbol;
            if (named != null && named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            {
                isNullable = true;
                return named.TypeArguments[0];
            }

            return type;
        }
    }
}
