using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Crops.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Shared.Domain.Repositories;

namespace GreenTech.SkyCrop.Platform.Crops.Domain.Repositories;

public interface ICropRepository : IBaseRepository<Crop>
{
    Task<Crop?> FindCropByIdAsync(CropId id, CancellationToken cancellationToken);
    
    Task<Crop?> FindCropByIdAsNoTrackingAsync(CropId cropId, CancellationToken cancellationToken);
}