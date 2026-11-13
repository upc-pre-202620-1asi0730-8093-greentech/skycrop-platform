using System.Net.Mime;
using GreenTech.SkyCrop.Platform.Crops.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Crops.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Crops.Interfaces.Rest.Resources;
using GreenTech.SkyCrop.Platform.Crops.Interfaces.Rest.Transform;
using GreenTech.SkyCrop.Platform.Resources.Errors;
using GreenTech.SkyCrop.Platform.Shared.Interfaces.Rest.ProblemDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace GreenTech.SkyCrop.Platform.Crops.Interfaces.Rest;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Available Crop Endpoints.")]
public class CropsController(
    ICropCommandService  cropCommandService,
    ICropQueryServices cropQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory
    ) : ControllerBase
{
    [HttpGet("{CropId:int}")]
    [EndpointSummary("Get Crop by Id")]
    [EndpointDescription("Get a crop by its unique identifier.")]
    [ProducesResponseType(typeof(CropResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]

    public async Task<IActionResult> GetCropById(int cropId,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
        CancellationToken cancellationToken)
    {
        var getCropByIdQuery = new GetCropByIdQuery(cropId);
        var crop = await cropQueryService.Handle(getCropByIdQuery, cancellationToken);
        
        return CropActionResultAssembler.ToActionResultFromGetCropByIdResult(
            this,
            crop,
            errorLocalizer,
            problemDetailsFactory,
            foundCrop => Ok(CropResourceFromEntityAssembler.ToResourceFromEntity(foundCrop))
            );
    }

    [HttpPost]
    [EndpointSummary("Create Crop")]
    [EndpointDescription("Create a new crop.")]
    [ProducesResponseType(typeof(CropResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]

    public async Task<IActionResult> CreateCrop(CreateCropResource resource,
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage, 
        CancellationToken cancellationToken)
    {
        var createCropCommand = CreateCropCommandFromResourceAssembler.ToCommandFromResource(resource);
        var result = await cropCommandService.Handle(createCropCommand, cancellationToken);

        return CropActionResultAssembler.ToActionResultFromCreateCropResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            createdCrop => CreatedAtAction(nameof(GetCropById), new { cropId = createdCrop.Id },
                CropResourceFromEntityAssembler.ToResourceFromEntity(createdCrop))
        );
    }

    [HttpGet]
    [EndpointSummary("Get all Crops")]
    [EndpointDescription("Get all crops.")]
    [ProducesResponseType(typeof(IEnumerable<CropResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCrops(
        [FromHeader(Name = "Accept-Language")] string? acceptLanguage,
        CancellationToken cancellationToken)
    {
        var getAllCropsQuery = new GetAllCropsQuery();
        var crops = await cropQueryService.Handle(getAllCropsQuery, cancellationToken);
        var cropsResources = crops.Select(CropResourceFromEntityAssembler.ToResourceFromEntity);
        return Ok(cropsResources);
    }
}