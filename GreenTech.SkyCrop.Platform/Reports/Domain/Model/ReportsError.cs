namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model;

public enum ReportsError
{
    None,
    ReportNotFound,
    ReportAlreadyRegistered,
    InvalidReportData,
    OperationCancelled,
    DatabaseError,
    InternalServerError
}