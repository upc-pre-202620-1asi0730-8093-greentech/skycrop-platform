using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;

namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.Errors;

/// <summary>A persistence race translated to a stable application error.</summary>
public sealed class DronePersistenceConflictException(Error error, Exception innerException)
    : Exception(error.Message, innerException)
{
    public Error Error { get; } = error;
}
