using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Shared.Domain.Repositories;

namespace GreenTech.SkyCrop.Platform.Drones.Domain.Repositories;

public interface IDroneRepository : IBaseRepository<Drone>
{
    Task<Drone?> FindBySerialKeyAsync(string key, CancellationToken cancellationToken = default);
}
