using GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;

namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.Queries;

public record GetReportByIdQuery(ReportId ReportId)
{
    public GetReportByIdQuery(int reportId) : this(new ReportId(reportId))
    {
        
    }
}