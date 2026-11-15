namespace GreenTech.SkyCrop.Platform.Crops.Domain.Model;

public enum CropsError
{
    None,
    CropNotFound,
    CropAlreadyRegistered,
    InvalidCropData,
    OperationCancelled,
    DatabaseError,
    InternalServerError
}