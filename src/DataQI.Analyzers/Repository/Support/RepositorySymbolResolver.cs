using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using DataQI.Analyzers.Completion.Extensions;
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

        public IReadOnlyList<IPropertySymbol> GetQueryableProperties(ITypeSymbol entityType)
        {
            Assert.NotNull(entityType, "EntityType must not be null");

            var properties = new List<IPropertySymbol>();
            var names = new HashSet<string>();

            for (var current = entityType; current != null; current = current.BaseType)
            {
                foreach (var property in current.GetMembers().OfType<IPropertySymbol>().Where(IsQueryable))
                {
                    // Walking from the most derived type, the first occurrence of a name wins.
                    if (names.Add(property.Name))
                        properties.Add(property);
                }
            }

            return properties;
        }

        private static bool IsQueryable(IPropertySymbol property)
            => !property.IsStatic
                && !property.IsIndexer
                && property.DeclaredAccessibility == Accessibility.Public
                && property.GetMethod != null
                && property.GetMethod.DeclaredAccessibility == Accessibility.Public
                && !property.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == "NotMappedAttribute")
                && property.Type.IsScalar();
    }
}
