using GreenTech.SkyCrop.Platform.Shared.Domain.Repositories;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/// <summary>
///     Unit of work for the application.
/// </summary>
/// <param name="context">
///     The database context for the application
/// </param>
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}