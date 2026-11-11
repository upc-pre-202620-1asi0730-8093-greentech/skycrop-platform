using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;

namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.Errors;

public class ReportErrors
{
    public static readonly Error ReportNotFound = Error.NotFound("Reports.ReportNotFound", "The specified report was not found.");
    
    public static readonly Error ReportAlreadyRegistered = Error.Conflict("Reports.ReportAlreadyRegistered", "A report with the provided Id was already registered.");
    
    public static readonly Error InvalidReportData = Error.Validation("Reports.InvalidReportData", "The report data provided was not valid.");
    
    public static readonly Error ReportCreationFailed = Error.Failure("Reports.ReportCreationFailed", "An error occurred while creating the report.");
}