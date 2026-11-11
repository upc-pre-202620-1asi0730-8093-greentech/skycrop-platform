using GreenTech.SkyCrop.Platform.Reports.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Reports.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;
using GreenTech.SkyCrop.Platform.Shared.Domain.Repositories;

namespace GreenTech.SkyCrop.Platform.Reports.Application.Internal.CommandServices;

/// <summary>
/// Report command service implementation
/// </summary>
public class ReportCommandService(
    IReportRepository reportRepository,
    IUnitOfWork unitOfWork
    ) : IReportCommandService
{
    
    /// <inheritdoc/>
    public async Task<Result<Report>> Handle(CreateReportCommand command, CancellationToken cancellationToken)
    {
        var report = new Report(command);
        await reportRepository.AddAsync(report, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<Report>.Success(report);
    }
}