using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using DataQI.Analyzers.Util;

namespace DataQI.Analyzers.Repository.Support
{
    public class RepositorySymbolResolver : IRepositorySymbolResolver
    {
        private const string CrudRepositoryName = "ICrudRepository";
        private const string CrudRepositoryNamespace = "DataQI.Commons.Repository";

        public bool TryResolveEntityType(SyntaxToken identifierToken, SemanticModel semanticModel, out ITypeSymbol entityType)
        {
            Assert.NotNull(semanticModel, "SemanticModel must not be null");

            entityType = null;

            var interfaceDeclaration = identifierToken.Parent?
                .AncestorsAndSelf()
                .OfType<InterfaceDeclarationSyntax>()
                .FirstOrDefault();

            if (interfaceDeclaration == null)
                return false;

            var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclaration) as INamedTypeSymbol;
            if (interfaceSymbol == null)
                return false;

            var candidates = interfaceSymbol.AllInterfaces.Concat(new[] { interfaceSymbol });

            foreach (var candidate in candidates)
            {
                if (candidate.Name != CrudRepositoryName || candidate.TypeArguments.Length != 2)
                    continue;

                if (candidate.ContainingNamespace?.ToDisplayString() != CrudRepositoryNamespace)
                    continue;

                entityType = candidate.TypeArguments[0];
                return true;
            }

            return false;
        }

        public IReadOnlyList<IPropertySymbol> GetPublicInstanceProperties(ITypeSymbol entityType)
        {
            Assert.NotNull(entityType, "EntityType must not be null");

            var properties = new List<IPropertySymbol>();
            var current = entityType;

            while (current != null)
            {
                properties.AddRange(current.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public));

                current = current.BaseType;
            }

            return properties;
        }
    }
}
