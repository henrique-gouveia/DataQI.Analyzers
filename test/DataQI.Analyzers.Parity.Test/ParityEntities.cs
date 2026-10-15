using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using DataQI.Analyzers.Repository.Support;

namespace DataQI.Analyzers.Parity.Test
{
    internal static class ParityEntities
    {
        private const string Source = @"
using System;
public enum Status { Draft, Published }
public class Scalars
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Age { get; set; }
    public decimal Price { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Deleted { get; set; }
    public bool Active { get; set; }
    public bool? Confirmed { get; set; }
    public Guid Key { get; set; }
    public char Grade { get; set; }
    public byte[] Blob { get; set; }
    public Status State { get; set; }
    public Status? PreviousState { get; set; }
    public int? Quantity { get; set; }
}
public class Hostile
{
    public string Name { get; set; }
    public string Age { get; set; }
    public string InStock { get; set; }
    public string LinkedIn { get; set; }
    public string LikeCount { get; set; }
    public string BetweenDates { get; set; }
    public string StartsWithFlag { get; set; }
    public string TermsAndConditions { get; set; }
    public string ItemIs { get; set; }
    public string ItemNot { get; set; }
    public string SalesAnd { get; set; }
    public string SalesOr { get; set; }
    public string TitleAsync { get; set; }
    public string OrderByDate { get; set; }
    public string PurchaseOrderBy { get; set; }
    public string Desc { get; set; }
    public string Asc { get; set; }
    public string ProductDesc { get; set; }
    public string CreatedBy { get; set; }
    public string Description { get; set; }
    public string Notes { get; set; }
    public string IsActive { get; set; }
}";

        public static IReadOnlyList<IPropertySymbol> Properties(string typeName)
        {
            var tree = CSharpSyntaxTree.ParseText(Source);
            var compilation = CSharpCompilation.Create("Parity", new[] { tree },
                new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

            return new RepositorySymbolResolver().GetQueryableProperties(compilation.GetTypeByMetadataName(typeName));
        }
    }
}
