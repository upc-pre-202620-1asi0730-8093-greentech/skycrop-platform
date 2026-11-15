using System.Net.Mime;
using GreenTech.SkyCrop.Platform.Plots.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Plots.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Plots.Interfaces.Rest.Resources;
using GreenTech.SkyCrop.Platform.Plots.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;

namespace GreenTech.SkyCrop.Platform.Plots.Interfaces.Rest;

[ApiController]
[Route("api/v1/plots")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Plots")]
public sealed class PlotsController(IAgriculturalPlotQueryService queries, IAgriculturalPlotCommandService commands)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AgriculturalPlotResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var plots = await queries.Handle(new GetAllPlotsQuery(), cancellationToken);
        return Ok(plots.Select(PlotResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(typeof(AgriculturalPlotResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var plot = await queries.Handle(new GetPlotByIdQuery(id), cancellationToken);
        return plot is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: PlotErrors.NotFound.Code,
                detail: PlotErrors.NotFound.Message)
            : Ok(PlotResourceFromEntityAssembler.ToResourceFromEntity(plot));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AgriculturalPlotResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterPlotResource resource, CancellationToken cancellationToken)
    {
        var command = PlotCommandFromResourceAssembler.ToCommandFromResource(resource);
        var result = await commands.Handle(command, cancellationToken);
        return result.Match<IActionResult>(
            plot => CreatedAtAction(nameof(GetById), new { id = plot.Id },
                PlotResourceFromEntityAssembler.ToResourceFromEntity(plot)),
            error => Problem(statusCode: StatusCodes.Status400BadRequest, title: error.Code, detail: error.Message));
    }
}
