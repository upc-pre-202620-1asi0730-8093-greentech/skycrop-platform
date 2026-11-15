using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;

namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.Errors;

public static class DroneErrors
{
    public static Error NotFound(string resource) => Error.NotFound($"Drones.{resource}NotFound", $"The {resource} was not found.");
    public static readonly Error DuplicateSerial = Error.Conflict("Drones.DuplicateSerial", "A drone with this serial number already exists.");
    public static readonly Error ActiveFlight = Error.Conflict("Drones.ActiveFlight", "The drone or route already has an active flight.");
    public static readonly Error ConcurrentUpdate = Error.Conflict("Drones.ConcurrentUpdate", "The resource changed concurrently. Reload it before retrying.");
    public static Error InvalidData(string message) => Error.Validation("Drones.InvalidData", message);
    public static Error InvalidOperation(string message) => Error.Conflict("Drones.InvalidOperation", message);
}
