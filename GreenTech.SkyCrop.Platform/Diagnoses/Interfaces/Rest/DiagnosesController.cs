using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest.Resources;
using GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest.Transform;
using GreenTech.SkyCrop.Platform.Diagnoses.Resources;
using Microsoft.AspNetCore.Mvc;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest;

[ApiController]
[Route("api/v1/diagnoses")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Diagnoses")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
public sealed class DiagnosesController(
    IDiagnosisCommandService commands,
    IDiagnosisQueryService queries) : ControllerBase
{
    /// <summary>
    /// Lists diagnoses, optionally filtered by a historical plot identifier.
    /// </summary>
    [HttpGet]
    [EndpointSummary("List diagnoses")]
    [EndpointDescription(
        "Returns diagnoses, optionally filtered by historical plotId. " +
        "Results are ordered by monitoring date, newest first.")]
    [ProducesResponseType(
        typeof(IEnumerable<DiagnosisResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery, Range(1, int.MaxValue,
            ErrorMessageResourceType = typeof(DiagnosesMessages),
            ErrorMessageResourceName = nameof(DiagnosesMessages.RangeField))]
        int? plotId,
        CancellationToken cancellationToken)
    {
        var result = plotId.HasValue
            ? await queries.Handle(
                new GetDiagnosesByPlotIdQuery(plotId.Value),
                cancellationToken)
            : await queries.Handle(
                new GetAllDiagnosesQuery(),
                cancellationToken);

        return result.Match<IActionResult>(
            diagnoses => Ok(diagnoses
                .Select(DiagnosisResourceFromEntityAssembler.ToResourceFromEntity)
                .ToArray()),
            error => DiagnosesActionResultAssembler.ToProblem(error));
    }

    /// <summary>
    /// Retrieves a diagnosis accessible to the current caller.
    /// </summary>
    [HttpGet("{id:int}")]
    [EndpointSummary("Get diagnosis by ID")]
    [EndpointDescription(
        "Returns a diagnosis and its anomalies. Inaccessible and nonexistent " +
        "diagnoses return the same not-found response.")]
    [ProducesResponseType(typeof(DiagnosisResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [Range(1, int.MaxValue,
            ErrorMessageResourceType = typeof(DiagnosesMessages),
            ErrorMessageResourceName = nameof(DiagnosesMessages.RangeField))]
        int id,
        CancellationToken cancellationToken)
    {
        var result = await queries.Handle(
            new GetDiagnosisByIdQuery(id),
            cancellationToken);

        return result.Match<IActionResult>(
            diagnosis => Ok(
                DiagnosisResourceFromEntityAssembler.ToResourceFromEntity(
                    diagnosis)),
            error => DiagnosesActionResultAssembler.ToProblem(error));
    }
    
    /// <summary>
    /// Retrieves georeferenced anomalies and their severity legend.
    /// </summary>
    [HttpGet("{id:int}/map")]
    [EndpointSummary("Get diagnosis map data")]
    [EndpointDescription(
        "Returns georeferenced anomaly points and a severity legend. " +
        "Map status is AVAILABLE, PARTIAL or UNAVAILABLE. " +
        "Missing coordinates are never fabricated. " +
        "An existing diagnosis without map data returns 200 with " +
        "UNAVAILABLE status. Results retain their simulated source.")]
    [ProducesResponseType(
        typeof(DiagnosisMapResource), StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMap(
        [Range(1, int.MaxValue,
            ErrorMessageResourceType = typeof(DiagnosesMessages),
            ErrorMessageResourceName = nameof(DiagnosesMessages.RangeField))]
        int id,
        CancellationToken cancellationToken)
    {
        var result = await queries.Handle(
            new GetDiagnosisByIdQuery(id),
            cancellationToken);

        return result.Match<IActionResult>(
            diagnosis => Ok(
                DiagnosisMapResourceFromEntityAssembler
                    .ToResourceFromEntity(diagnosis)),
            error => DiagnosesActionResultAssembler.ToProblem(error));
    }

    /// <summary>
    /// Registers a diagnosis and performs simulated analysis.
    /// Inspect status: a registered diagnosis may have failed analysis.
    /// </summary>
    [HttpPost]
    [EndpointSummary("Generate a simulated diagnosis")]
    [EndpointDescription(
        "Only scenario is required. No plot, flight or images are needed. " +
        "Supported scenarios: HEALTHY, WATER_STRESS and PEST. " +
        "A 201 response confirms registration; inspect status to determine " +
        "whether analysis completed or failed. No real images are analyzed.")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(DiagnosisResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateDiagnosisResource resource,
        CancellationToken cancellationToken)
    {
        var command = new GenerateDiagnosisCommand(
            resource.Scenario!);

        var result = await commands.Handle(command, cancellationToken);

        return result.Match<IActionResult>(
            diagnosis => CreatedAtAction(
                nameof(GetById),
                new { id = diagnosis.Id },
                DiagnosisResourceFromEntityAssembler.ToResourceFromEntity(
                    diagnosis)),
            error => DiagnosesActionResultAssembler.ToProblem(error));
    }
}
