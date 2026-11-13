using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;

namespace GreenTech.SkyCrop.Platform.Crops.Domain.Model.Errors;

public static class CropErrors
{
    public static readonly Error NotFound = Error.NotFound("Crops.NotFound", "Crop not found");
    public static Error InvalidData(string field) => Error.Validation("Crops.InvalidData", field);
}