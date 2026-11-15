using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Drones.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GreenTech.SkyCrop.Platform.Drones.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

public sealed class AerialImageRepository(AppDbContext context) : BaseRepository<AerialImage>(context), IAerialImageRepository
{

}
