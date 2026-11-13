using GreenTech.SkyCrop.Platform.Crops.Domain.Model.ValueObjects;

namespace GreenTech.SkyCrop.Platform.Crops.Domain.Model.Queries;

public record GetCropByIdQuery(CropId CropId)
{
    public GetCropByIdQuery(int CropId) : this(new CropId(CropId))
    {
        
    }
};