using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Fuji;

public class DiagnosticReporter
{
    private static readonly DiagnosticDescriptor MissingServicesDiagnostic = new DiagnosticDescriptor("PG0001",
        "Missing required service",
        "Provide required services '{0}' for type '{1}' to generate '{2}'", "Usage", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor DuplicateServicesDiagnostic = new DiagnosticDescriptor("PG0002",
        "Duplicate services",
        "Duplicate services '{0}' are not supported for type '{1}'", "Usage", DiagnosticSeverity.Error, true);
    private readonly SourceProductionContext _sourceProductionContext;

    public DiagnosticReporter(SourceProductionContext sourceProductionContext)
    {
        _sourceProductionContext = sourceProductionContext;
    }

    public bool HasError { get; private set; }

    public void ReportDuplicateServices(ITypeSymbol providerType,
        ImmutableArray<ITypeSymbol> duplicateServices)
    {
        _sourceProductionContext.ReportDiagnostic(Diagnostic.Create(DuplicateServicesDiagnostic, Location.None,
            string.Join(", ", duplicateServices.Select(missingType => missingType.ToDisplayString())),
            providerType.ToDisplayString()));
        HasError = true;
    }

    public void ReportMissingServices(ITypeSymbol providerType, ITypeSymbol serviceType,
        ImmutableArray<(ITypeSymbol Symbol, string? Key)> missingServices, Location location)
    {
        _sourceProductionContext.ReportDiagnostic(Diagnostic.Create(MissingServicesDiagnostic, location,
            string.Join(", ", missingServices.Select(missingType => $"{missingType.Symbol.ToDisplayString()}{(missingType.Key != null ? $"({missingType.Key})" : null)}")),
            serviceType.ToDisplayString(), providerType.ToDisplayString()));
        HasError = true;
    }
}