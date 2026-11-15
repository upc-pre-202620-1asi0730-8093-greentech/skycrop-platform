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
[Route("api/v1/aerialImages")]
[Route("api/v1/aerial-images")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("AerialImages")]
public sealed class AerialImagesController(IDroneQueryService queries) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AerialImageResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var entities = await queries.Handle(new GetAllAerialImagesQuery(), cancellationToken);
        return Ok(entities.Select(DroneResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{id:int:min(1)}")]
    [ProducesResponseType(typeof(AerialImageResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var entity = await queries.Handle(new GetAerialImageByIdQuery(id), cancellationToken);
        return entity is null
            ? DronesActionResultAssembler.ToProblem(this, DroneErrors.NotFound("AerialImage"))
            : Ok(DroneResourceFromEntityAssembler.ToResourceFromEntity(entity));
    }
}
