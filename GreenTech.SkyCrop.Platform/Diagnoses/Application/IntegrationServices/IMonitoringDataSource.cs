using GreenTech.SkyCrop.Platform.Shared.Application.Model;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;

public interface IMonitoringDataSource
{
    Task<Result<MonitoringData>> GetAsync(
        int plotId,
        int flightId,
        IReadOnlyList<int> imageIds,
        CancellationToken cancellationToken = default);
}