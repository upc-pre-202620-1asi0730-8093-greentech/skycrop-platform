using GreenTech.SkyCrop.Platform.Reports.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Reports.Domain.Repositories;

namespace GreenTech.SkyCrop.Platform.Reports.Application.Internal.QueryServices;

public class ReportQueryService(IReportRepository reportRepository) : IReportQueryService
{
    public async Task<Report?> Handle(GetReportByIdQuery query, CancellationToken cancellationToken)
    {
        return await reportRepository.FindReportByIdAsNoTrackingAsync(query.ReportId, cancellationToken);
    }
}