using System.Net.Mime;
using GreenTech.SkyCrop.Platform.Reports.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Reports.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Reports.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Reports.Interfaces.Rest.Resources;
using GreenTech.SkyCrop.Platform.Reports.Interfaces.Rest.Transform;
using GreenTech.SkyCrop.Platform.Resources.Errors;
using GreenTech.SkyCrop.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace GreenTech.SkyCrop.Platform.Reports.Interfaces.Rest;

[ApiController]
[Route("api/v1/reports")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Reports")]
public sealed class ReportsController(
    IReportCommandService reportCommandService,
    IReportQueryService reportQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory
    ) : ControllerBase
{
    [HttpGet("{reportId:int}")]
    [EndpointSummary("Get report by Id")]
    [EndpointDescription("Get a report by its unique identifier")]
    [ProducesResponseType(typeof(ReportResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportById(
        int reportId,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
        CancellationToken cancellationToken
    )
    {
        var getReportByIdQuery = new GetReportByIdQuery(reportId);
        var report = await reportQueryService.Handle(getReportByIdQuery, cancellationToken);

        return ReportsActionResultAssembler.ToActionResultFromGetReportByIdResult(
            this,
            report,
            errorLocalizer,
            problemDetailsFactory,
            foundReport => Ok(ReportResourceFromEntityAssembler.ToResourceFromEntity(foundReport))
        );
    }


    [HttpPost]
    [EndpointSummary("Create a Report")]
    [EndpointDescription("Create a new Report")]
    [ProducesResponseType(typeof(ReportResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateReport(
        CreateReportResource resource,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
        CancellationToken cancellationToken
    )
    {
        var createReportCommand = CreateReportCommandFromResourceAssembler.ToCommandFromResource(resource);
        var result = await reportCommandService.Handle(createReportCommand, cancellationToken);

        return ReportsActionResultAssembler.ToActionResultFromCreateReportResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            createdReport => CreatedAtAction(nameof(GetReportById), new { reportId = createdReport.Id },
                ReportResourceFromEntityAssembler.ToResourceFromEntity(createdReport))
        );
    }


    [HttpGet]
    [EndpointSummary("Get all reports")]
    [EndpointDescription("Get all registered reports")]
    [ProducesResponseType(typeof(IEnumerable<ReportResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllReports(
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
        CancellationToken cancellationToken
    )
    {
        var getAllReportsQuery = new GetAllReportsQuery();
        var reports = await reportQueryService.Handle(getAllReportsQuery, cancellationToken);
        var reportResources = reports.Select(ReportResourceFromEntityAssembler.ToResourceFromEntity);
        return Ok(reportResources);
    }
}

