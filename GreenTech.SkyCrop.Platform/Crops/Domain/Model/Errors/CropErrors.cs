using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;

namespace GreenTech.SkyCrop.Platform.Crops.Domain.Model.Errors;

public class CropErrors
{
    public static readonly Error CropNotFound = Error.NotFound("Crops.NotFound", "The specified Crop was not found");
    
    public static readonly Error CropAlreadyRegistered = Error.Conflict("Crops.CropAlreadyRegistered", "A crop with the provided Id was already registered.");
    
    public static readonly Error InvalidCropData = Error.Validation("Crops.InvalidCropData", "The crop data provided was not valid.");
    
    public static readonly Error CropCreationFailed = Error.Failure("Crops.CropCreationFailed", "An error occurred while creating the crop.");
}