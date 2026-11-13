using System.Net;
using GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;
using GreenTech.SkyCrop.Platform.Shared.Application.Model;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;
using Microsoft.AspNetCore.Http;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Infrastructure.Integration;

/// <summary>
/// Local demonstration permissions for one configured plot.
/// Must only be registered in Development against the demonstration database.
/// </summary>
public sealed class LocalDemoPlotAccessService(
    int plotId,
    IHttpContextAccessor httpContextAccessor) : IPlotAccessService
{
    public Task<Result> CheckAccessAsync(
        int requestedPlotId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsLocalRequest())
            return Task.FromResult(Result.Failure(LocalOnly()));

        if (requestedPlotId != plotId || plotId <= 0)
        {
            return Task.FromResult(Result.Failure(Error.Forbidden(
                "Diagnoses.PlotAccessDenied",
                "This plot is not available in the local demonstration.")));
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result<IReadOnlyCollection<int>>> GetAccessiblePlotIdsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsLocalRequest())
        {
            return Task.FromResult(
                Result<IReadOnlyCollection<int>>.Failure(LocalOnly()));
        }

        IReadOnlyCollection<int> ids = plotId > 0
            ? new[] { plotId }
            : Array.Empty<int>();

        return Task.FromResult(
            Result<IReadOnlyCollection<int>>.Success(ids));
    }

    private bool IsLocalRequest()
    {
        var address = httpContextAccessor.HttpContext?
            .Connection.RemoteIpAddress;

        if (address is null)
            return false;

        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        return IPAddress.IsLoopback(address);
    }

    private static Error LocalOnly() => Error.Forbidden(
        "Diagnoses.LocalDemoOnly",
        "The demonstration is available only from the local machine.");
}