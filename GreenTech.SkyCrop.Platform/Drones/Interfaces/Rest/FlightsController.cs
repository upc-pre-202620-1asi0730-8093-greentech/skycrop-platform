using System.Net.Mime;
using GreenTech.SkyCrop.Platform.Drones.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Drones.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Resources;
using GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;

namespace GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest;

[ApiController]
[Route("api/v1/flights")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Flights")]
public sealed class FlightsController(IDroneQueryService queries, IDroneCommandService commands) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FlightResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var entities = await queries.Handle(new GetAllFlightsQuery(), cancellationToken);
        return Ok(entities.Select(DroneResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(typeof(FlightResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var entity = await queries.Handle(new GetFlightByIdQuery(id), cancellationToken);
        return entity is null
            ? DronesActionResultAssembler.ToProblem(this, DroneErrors.NotFound("Flight"))
            : Ok(DroneResourceFromEntityAssembler.ToResourceFromEntity(entity));
    }

    [HttpPost]
    [ProducesResponseType(typeof(FlightResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Plan(PlanFlightResource resource, CancellationToken cancellationToken) =>
        DronesActionResultAssembler.Handle(this,
            () => commands.Handle(DroneCommandFromResourceAssembler.ToCommandFromResource(resource), cancellationToken),
            entity => CreatedAtAction(nameof(GetById), new { id = entity.Id }, DroneResourceFromEntityAssembler.ToResourceFromEntity(entity)));

    [HttpPatch("{id:int:min(1)}")]
    [ProducesResponseType(typeof(FlightResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Cancel(int id, CancelFlightResource resource, CancellationToken cancellationToken) =>
        DronesActionResultAssembler.Handle(this,
            () => commands.Handle(DroneCommandFromResourceAssembler.ToCommandFromResource(id, resource), cancellationToken),
            entity => Ok(DroneResourceFromEntityAssembler.ToResourceFromEntity(entity)));
}
