using GreenTech.SkyCrop.Platform.Crops.Application.QueryServices;
using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Queries;
using GreenTech.SkyCrop.Platform.Crops.Domain.Repositories;

namespace GreenTech.SkyCrop.Platform.Crops.Application.Internal.QueryServices;

public class CropQueryService(ICropRepository cropRepository) : ICropQueryServices
{
    public async Task<Crop?> Handle(GetCropByIdQuery query, CancellationToken cancellationToken)
    {
        return await cropRepository.FindCropByIdAsNoTrackingAsync(query.CropId, cancellationToken);
    }
}