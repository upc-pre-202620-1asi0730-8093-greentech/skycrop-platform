using GreenTech.SkyCrop.Platform.Crops.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Events;

namespace GreenTech.SkyCrop.Platform.Crops.Domain.Model.Events;

public record CropCreatedEvent(CropId CropId) : IEvent;