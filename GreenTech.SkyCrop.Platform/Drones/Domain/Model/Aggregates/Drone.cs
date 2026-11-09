using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Entities;

namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;

public sealed class Drone : AggregateRoot, IAuditableEntity
{
    private Drone() { }
    public Drone(CreateDroneCommand command)
    {
        if (command.Status != nameof(EDroneStatus.DISCONNECTED))
            throw new ArgumentException("New drones must be registered as DISCONNECTED.");
        SetInformation(command.Name, command.Model, command.SerialNumber, command.Camera,
            command.AutonomyMinutes, command.Battery, command.ImageUrl);
    }
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public DroneSerialNumber SerialNumber { get; private set; } = null!;
    public string SerialNumberKey { get; private set; } = null!;
    public string Camera { get; private set; } = null!;
    public decimal AutonomyMinutes { get; private set; }
    public BatteryLevel Battery { get; private set; } = null!;
    public EDroneStatus Status { get; private set; } = EDroneStatus.DISCONNECTED;
    public string ImageUrl { get; private set; } = string.Empty;
    public Guid Version { get; private set; } = Guid.NewGuid();
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public void Update(UpdateDroneCommand command)
    {
        var status = command.Status is null ? Status : DroneValidation.Status<EDroneStatus>(command.Status);
        if (command.Status is not null &&
            (Status is not (EDroneStatus.DISCONNECTED or EDroneStatus.AVAILABLE) ||
             status is not (EDroneStatus.DISCONNECTED or EDroneStatus.AVAILABLE)))
            throw new DroneStateException("Connection changes are only allowed between DISCONNECTED and AVAILABLE.");
        SetInformation(command.Name ?? Name, command.Model ?? Model, command.SerialNumber ?? SerialNumber.Value,
            command.Camera ?? Camera, command.AutonomyMinutes ?? AutonomyMinutes,
            command.Battery ?? Battery.Value, command.ImageUrl ?? ImageUrl);
        Status = status;
        Touch();
    }

    /// <summary>Changes the concurrency token when a flight reserves this drone.</summary>
    public void Touch() => Version = Guid.NewGuid();

    private void SetInformation(string name, string model, string serialNumber, string camera,
        decimal autonomyMinutes, decimal battery, string? imageUrl)
    {
        // Validate the full proposed state before mutating the aggregate.
        var nextName = DroneValidation.RequiredText(name, nameof(name));
        var nextModel = DroneValidation.RequiredText(model, nameof(model));
        var nextSerial = new DroneSerialNumber(serialNumber);
        var nextCamera = DroneValidation.RequiredText(camera, nameof(camera));
        DroneValidation.PositiveMeasurement(autonomyMinutes, nameof(autonomyMinutes));
        var nextBattery = new BatteryLevel(battery);
        var nextImage = DroneValidation.HttpUrl(imageUrl);
        Name = nextName;
        Model = nextModel;
        SerialNumber = nextSerial;
        SerialNumberKey = nextSerial.Key;
        Camera = nextCamera;
        AutonomyMinutes = autonomyMinutes;
        Battery = nextBattery;
        ImageUrl = nextImage;
    }
}
