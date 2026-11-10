using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Queries;

namespace GreenTech.SkyCrop.Platform.Reports.Application.QueryServices;

public interface IReportQueryService
{
    Task<Report?> Handle(GetReportByIdQuery query, CancellationToken cancellationToken);
}