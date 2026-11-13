using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Crops.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;

namespace GreenTech.SkyCrop.Platform.Crops.Application.CommandServices;

public interface ICropCommandService
{
    Task <Result<Crop>?> Handle(CreateCropCommand command, CancellationToken cancellationToken);
}