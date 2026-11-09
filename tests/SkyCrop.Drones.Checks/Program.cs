using GreenTech.SkyCrop.Platform.Drones.Domain.Model;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.Errors;
using GreenTech.SkyCrop.Platform.Drones.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Drones.Interfaces.Rest.Transform;
using GreenTech.SkyCrop.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;

var count = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    count++;
    Console.WriteLine($"PASS {name}");
}
void Invalid(Action action, string name)
{
    try { action(); }
    catch (ArgumentException) { Check(true, name); return; }
    throw new Exception(name);
}
void Conflict(Action action, string name)
{
    try { action(); }
    catch (DroneStateException) { Check(true, name); return; }
    throw new Exception(name);
}

CreateDroneCommand Command() => new("Field drone", "DJI", " sk-123 ", "48 MP", 30, 100);
var drone = new Drone(Command());
Check(drone.SerialNumber.Value == "sk-123" && drone.SerialNumber.Key == "SK-123", "serial trimmed with a case-insensitive uniqueness key");
Invalid(() => new Drone(Command() with { Name = " " }), "blank name rejected");
Invalid(() => new Drone(Command() with { AutonomyMinutes = 0 }), "zero autonomy rejected");
Invalid(() => new Drone(Command() with { AutonomyMinutes = decimal.MaxValue }), "unsupported storage precision rejected");
Invalid(() => new Drone(Command() with { Battery = 101 }), "battery over 100 rejected");
Invalid(() => new Drone(Command() with { Battery = -1 }), "negative battery rejected");
Invalid(() => new Drone(Command() with { ImageUrl = "javascript:alert(1)" }), "unsafe image URL rejected");
Invalid(() => new Drone(Command() with { Status = "ON_MISSION" }), "registration cannot spoof a mission state");
var version = drone.Version;
Invalid(() => drone.Update(new UpdateDroneCommand(1, Name: "Changed", Battery: -1)), "invalid update rejected");
Check(drone.Name == "Field drone" && drone.Version == version, "invalid update leaves aggregate unchanged");
drone.Update(new UpdateDroneCommand(1, Status: "AVAILABLE"));
Check(drone.Status == EDroneStatus.AVAILABLE && drone.Version != version, "connection changes status and concurrency token");
Invalid(() => drone.Update(new UpdateDroneCommand(1, Status: "1")), "numeric enum strings rejected");
Conflict(() => drone.Update(new UpdateDroneCommand(1, Status: "ON_MISSION")), "connection endpoint cannot start a real flight");

RouteWaypoint[] Points() => [new(0, -11.380296, -73.922818), new(1, -11.388121, -73.920757)];
var route = new FlightRoute(new CreateFlightRouteCommand("Route 1", 3, 30, 4.4m, Points()));
Invalid(() => new RouteWaypoint(0, double.NaN, 0), "non-finite coordinates rejected");
Invalid(() => new RouteWaypoint(0, 91, 0), "latitude outside valid range rejected");
Invalid(() => new FlightParameters(30, 0), "zero route speed rejected");
Invalid(() => new FlightRoute(new CreateFlightRouteCommand("Route", 3, 30, 4, [new(0, 0, 0)])), "one-point route rejected");
var firstWaypoint = route.Waypoints.First();
route.Update(new UpdateFlightRouteCommand(1, Waypoints: [new(0, -10, -70), new(1, -9, -69), new(2, -8, -68)]));
Check(ReferenceEquals(firstWaypoint, route.Waypoints.First()) && firstWaypoint.Latitude == -10, "route update retains owned entity identity");
route.Update(new UpdateFlightRouteCommand(1, Name: "Updated route"));
Check(route.Waypoints.Count == 3, "partial route update preserves waypoints");
Invalid(() => route.Update(new UpdateFlightRouteCommand(1, Name: "Invalid change", PlotId: 0)), "invalid plot reference rejected");
Check(route.Name == "Updated route", "invalid route update is atomic");
Invalid(() => new AerialImage(0, "https://example.com/image.jpg", DateTimeOffset.UtcNow), "image requires a flight reference");
Invalid(() => new AerialImage(1, "file:///image.jpg", DateTimeOffset.UtcNow), "image requires HTTP location");
var json = System.Text.Json.JsonSerializer.Serialize(DroneResourceFromEntityAssembler.ToResourceFromEntity(drone),
    new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
Check(json.Contains("\"serialNumber\":\"sk-123\"") && json.Contains("\"battery\":100"), "REST resource matches frontend primitive fields");
Check(!json.Contains("serialNumberKey") && !json.Contains("version"), "internal concurrency and uniqueness fields are not exposed");

using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
    .UseMySQL("server=127.0.0.1;database=model_only;user=unused;password=unused;").Options);
var model = context.Model;
var droneMapping = model.FindEntityType(typeof(Drone))!;
Check(droneMapping.GetTableName() == "drones", "drone table configured");
Check(droneMapping.GetIndexes().Any(index => index.IsUnique && index.Properties.Single().Name == "SerialNumberKey"), "unique serial index configured");
var flightMapping = model.FindEntityType(typeof(Flight))!;
Check(flightMapping.GetIndexes().Any(index => index.IsUnique && index.Properties.Single().Name == "ActiveDroneId"), "one active flight constraint configured");
Check(flightMapping.FindProperty("Version")!.IsConcurrencyToken && droneMapping.FindProperty("Version")!.IsConcurrencyToken, "planning and connection use concurrency tokens");
Check(flightMapping.GetForeignKeys().Count() == 2, "flight references persisted drone and route");
// Optional persistence check against a disposable database after the HTTP lifecycle checks.
if (Environment.GetEnvironmentVariable("SKYCROP_TEST_CONNECTION") is { Length: > 0 } connection &&
    int.TryParse(Environment.GetEnvironmentVariable("SKYCROP_TEST_FLIGHT_ID"), out var testFlightId))
{
    await using var database = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseMySQL(connection).Options);
    var image = new AerialImage(testFlightId, "https://example.com/drones-check-image.jpg", DateTimeOffset.UtcNow);
    database.Add(image);
    await database.SaveChangesAsync();
    var saved = await database.Set<AerialImage>().AsNoTracking().SingleAsync(item => item.Id == image.Id);
    Check(saved.FlightId == testFlightId && saved.FileUrl == image.FileUrl, "image metadata persists with the flight foreign key");
    var resource = DroneResourceFromEntityAssembler.ToResourceFromEntity(saved);
    Check(resource.ImageUrl == saved.FileUrl, "image resource exposes imageUrl expected by the frontend");
}
Console.WriteLine($"Completed {count} domain and persistence-model checks.");
