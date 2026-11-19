using GreenTech.SkyCrop.Platform.Diagnoses.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Application.Internal.QueryServices;

public sealed class DiagnosisQueryService(
    IDiagnosisRepository repository) : IDiagnosisQueryService
{
    public async Task<Result<Diagnosis>> Handle(
        GetDiagnosisByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(query);

        if (query.DiagnosisId <= 0)
            return Result<Diagnosis>.Failure(DiagnosisErrors.InvalidDiagnosisId);

        var diagnosis = await repository.FindByIdAsNoTrackingAsync(
            query.DiagnosisId, cancellationToken);

        // IAM authorization will be integrated with the rest of the platform.
        if (diagnosis is null)
            return Result<Diagnosis>.Failure(DiagnosisErrors.NotFound);

        return Result<Diagnosis>.Success(diagnosis);
    }

    public async Task<Result<IEnumerable<Diagnosis>>> Handle(
        GetDiagnosesByPlotIdQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(query);

        if (query.PlotId <= 0)
            return Result<IEnumerable<Diagnosis>>.Failure(DiagnosisErrors.InvalidPlotId);

        var diagnoses = await repository.FindByPlotIdAsync(
            query.PlotId, cancellationToken);

        return Result<IEnumerable<Diagnosis>>.Success(diagnoses);
    }

    public async Task<Result<IEnumerable<Diagnosis>>> Handle(
        GetAllDiagnosesQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(query);

        var diagnoses = await repository.FindAllAsNoTrackingAsync(cancellationToken);

        return Result<IEnumerable<Diagnosis>>.Success(diagnoses);
    }
}