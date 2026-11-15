using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Entities;

namespace GreenTech.SkyCrop.Platform.Crops.Domain.Model.Aggregates;

public partial class Crop : IAuditableEntity
{
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}