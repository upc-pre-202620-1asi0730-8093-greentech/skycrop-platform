using GreenTech.SkyCrop.Platform.Crops.Application.CommandServices;
using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Crops.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;
using GreenTech.SkyCrop.Platform.Shared.Domain.Repositories;

namespace GreenTech.SkyCrop.Platform.Crops.Application.Internal.CommandServices;

public class CropCommandService(
    ICropRepository cropRepository, 
    IUnitOfWork unitOfWork) 
    : ICropCommandService
{
    public async Task<Result<Crop>?> Handle(CreateCropCommand command, CancellationToken cancellationToken)
    {
        Crop crop;
        try
        {
            crop = new Crop(command);
        }
        catch (ArgumentException exception)
        {
            return Result<Crop>.Failure(CropErrors.InvalidData(exception.Message));
        }

        await cropRepository.AddAsync(crop, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken); 
        return Result<Crop>.Success(crop); 
    }
}