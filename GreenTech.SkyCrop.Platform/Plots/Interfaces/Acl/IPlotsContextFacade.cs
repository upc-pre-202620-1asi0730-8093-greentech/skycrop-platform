namespace GreenTech.SkyCrop.Platform.Plots.Interfaces.Acl;

/// <summary>Provides parcel lookups to other bounded contexts.</summary>
public interface IPlotsContextFacade
{
    Task<bool> PlotExistsAsync(int plotId, CancellationToken cancellationToken = default);
}
