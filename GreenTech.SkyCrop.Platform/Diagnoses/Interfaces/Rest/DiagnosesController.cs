using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest.Resources;
using GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest.Transform;
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
    /// Lists accessible diagnoses, optionally filtered by plot.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(
        typeof(IEnumerable<DiagnosisResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery, Range(1, int.MaxValue)] int? plotId,
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
    [ProducesResponseType(typeof(DiagnosisResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [Range(1, int.MaxValue)] int id,
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
    /// Registers a diagnosis and performs simulated analysis.
    /// Inspect status: a registered diagnosis may have failed analysis.
    /// </summary>
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(DiagnosisResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateDiagnosisResource resource,
        CancellationToken cancellationToken)
    {
        var command = new GenerateDiagnosisCommand(
            resource.PlotId,
            resource.FlightId,
            resource.ImageIds!,
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