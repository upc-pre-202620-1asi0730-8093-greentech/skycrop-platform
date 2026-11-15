using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Application.QueryServices;

/// <summary>
/// Defines diagnosis queries with access checks and explicit failures.
/// </summary>
public interface IDiagnosisQueryService
{
    Task<Result<Diagnosis>> Handle(
        GetDiagnosisByIdQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<Diagnosis>>> Handle(
        GetDiagnosesByPlotIdQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<Diagnosis>>> Handle(
        GetAllDiagnosesQuery query,
        CancellationToken cancellationToken = default);
}