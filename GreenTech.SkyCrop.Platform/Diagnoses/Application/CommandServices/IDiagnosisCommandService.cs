using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Application.CommandServices;

/// <summary>
/// Defines diagnosis generation use cases.
/// </summary>
public interface IDiagnosisCommandService
{
    Task<Result<Diagnosis>> Handle(
        GenerateDiagnosisCommand command,
        CancellationToken cancellationToken = default);
}