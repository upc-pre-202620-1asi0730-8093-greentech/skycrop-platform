using GreenTech.SkyCrop.Platform.Crops.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Crops.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Crops.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GreenTech.SkyCrop.Platform.Crops.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public class CropRepository(AppDbContext  context) : BaseRepository<Crop>(context), ICropRepository
{
    public async Task<Crop?> FindCropByIdAsync(CropId id, CancellationToken cancellationToken)
    {
        return await Context.Set<Crop>()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Crop?> FindCropByIdAsNoTrackingAsync(CropId cropId, CancellationToken cancellationToken)
    {
        return await Context.Set<Crop>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == cropId, cancellationToken);
    }
}