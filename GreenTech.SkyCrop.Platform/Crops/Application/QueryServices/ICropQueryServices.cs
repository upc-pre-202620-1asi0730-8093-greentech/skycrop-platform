using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Queries;

namespace GreenTech.SkyCrop.Platform.Crops.Application.QueryServices;

public interface ICropQueryServices
{
    Task<Crop?> Handle(GetCropByIdQuery query, CancellationToken cancellationToken);
}