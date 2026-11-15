using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Drones.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace GreenTech.SkyCrop.Platform.Drones.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>Commits the Drones context and translates database-enforced conflicts.</summary>
public sealed class DroneUnitOfWork(AppDbContext context) : IDroneUnitOfWork
{
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        try { await context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new DronePersistenceConflictException(DroneErrors.ConcurrentUpdate, exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is MySqlException { Number: 1205 or 1213 })
        {
            throw new DronePersistenceConflictException(DroneErrors.ConcurrentUpdate, exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is MySqlException { Number: 1062 })
        {
            var error = exception.InnerException.Message.Contains("serial_number_key", StringComparison.OrdinalIgnoreCase)
                ? DroneErrors.DuplicateSerial : DroneErrors.ActiveFlight;
            throw new DronePersistenceConflictException(error, exception);
        }
    }
}
