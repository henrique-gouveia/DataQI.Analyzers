using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace DataQI.Analyzers.Repository
{
    public interface IRepositorySymbolResolver
    {
        bool TryResolveEntityType(SyntaxToken identifierToken, SemanticModel semanticModel, out ITypeSymbol entityType);

        IReadOnlyList<IPropertySymbol> GetQueryableProperties(ITypeSymbol entityType);
    }
}
