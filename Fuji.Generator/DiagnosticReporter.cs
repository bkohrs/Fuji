using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Fuji;

public class DiagnosticReporter
{
    private static readonly DiagnosticDescriptor MissingServicesDiagnostic = new DiagnosticDescriptor("PG0001",
        "Missing required service",
        "Provide required services '{0}' for type '{1}' to generate '{2}'", "Usage", DiagnosticSeverity.Error, true);
    private readonly SourceProductionContext _sourceProductionContext;

    public DiagnosticReporter(SourceProductionContext sourceProductionContext)
    {
        _sourceProductionContext = sourceProductionContext;
    }

    public bool HasError { get; private set; }

    public void ReportMissingServices(ITypeSymbol providerType, ITypeSymbol serviceType,
        ImmutableArray<ITypeSymbol> missingServices, Location location)
    {
        _sourceProductionContext.ReportDiagnostic(Diagnostic.Create(MissingServicesDiagnostic, location,
            string.Join(", ", missingServices.Select(missingType => missingType.ToDisplayString())),
            serviceType.ToDisplayString(), providerType.ToDisplayString()));
        HasError = true;
    }
}