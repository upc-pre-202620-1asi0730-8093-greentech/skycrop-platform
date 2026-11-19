using System.Net;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Integration;

// Local development only, until IAM supplies the caller's plot permissions.
public sealed class DevelopmentPlotAccessService(
    AppDbContext context,
    IHttpContextAccessor httpContextAccessor) : IPlotAccessService
{
    public async Task<Result> CheckAccessAsync(int plotId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsLocalRequest()) return Result.Failure(LocalOnly());
        if (plotId <= 0)
            return Result.Failure(Error.Validation(
                "Diagnoses.InvalidPlotId", "A positive plot identifier is required."));

        var exists = await context.Set<AgriculturalPlot>().AsNoTracking()
            .AnyAsync(plot => plot.Id == plotId, cancellationToken);
        return exists ? Result.Success() : Result.Failure(Error.Forbidden(
            "Diagnoses.PlotAccessDenied", "Access to this plot is not allowed."));
    }

    public async Task<Result<IReadOnlyCollection<int>>> GetAccessiblePlotIdsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsLocalRequest()) return Result<IReadOnlyCollection<int>>.Failure(LocalOnly());

        var ids = await context.Set<AgriculturalPlot>().AsNoTracking()
            .Select(plot => plot.Id).ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<int>>.Success(ids);
    }

    private bool IsLocalRequest()
    {
        var address = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
        if (address is null) return false;
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        return IPAddress.IsLoopback(address);
    }

    private static Error LocalOnly() => Error.Forbidden(
        "Diagnoses.LocalDemoOnly", "Development access is available only from the local machine.");
}
