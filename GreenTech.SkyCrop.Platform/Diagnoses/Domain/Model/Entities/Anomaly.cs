namespace GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Entities;

/// <summary>
/// Represents an anomaly detected within a diagnosis.
/// Coordinates are optional and must be supplied together.
/// </summary>
public sealed class Anomaly
{
    // Required by Entity Framework Core.
    private Anomaly() { }

    public Anomaly(
        string type,
        string severity,
        string description,
        string location,
        double? latitude = null,
        double? longitude = null)
    {
        var normalizedType = RequiredText(type, 40, nameof(type))
            .ToUpperInvariant();

        var normalizedSeverity = RequiredText(severity, 20, nameof(severity))
            .ToUpperInvariant();

        // Normalize the legacy spelling also supported by the frontend.
        if (normalizedType == "FERTILIZER_DEFICIENCITY")
            normalizedType = "FERTILIZER_DEFICIENCY";

        if (normalizedType is not (
            "WATER_STRESS" or
            "PEST" or
            "FERTILIZER_DEFICIENCY" or
            "OTHER"))
        {
            throw new ArgumentException(
                "Unknown anomaly type.",
                nameof(type));
        }

        if (normalizedSeverity is not (
            "LOW" or "MEDIUM" or "HIGH" or "CRITICAL"))
        {
            throw new ArgumentException(
                "Unknown anomaly severity.",
                nameof(severity));
        }

        if ((latitude is null) != (longitude is null))
        {
            throw new ArgumentException(
                "Latitude and longitude must be supplied together.");
        }

        ValidateCoordinate(latitude, 90, nameof(latitude));
        ValidateCoordinate(longitude, 180, nameof(longitude));

        Type = normalizedType;
        Severity = normalizedSeverity;
        Description = RequiredText(description, 1000, nameof(description));
        Location = RequiredText(location, 255, nameof(location));
        Latitude = latitude;
        Longitude = longitude;
    }

    public int Id { get; private set; }

    public int DiagnosisId { get; private set; }

    public string Type { get; private set; } = null!;

    public string Severity { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public string Location { get; private set; } = null!;

    public double? Latitude { get; private set; }

    public double? Longitude { get; private set; }

    private static string RequiredText(
        string value,
        int maximumLength,
        string field)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, field);

        var text = value.Trim();

        if (text.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Use at most {maximumLength} characters.",
                field);
        }

        return text;
    }

    private static void ValidateCoordinate(
        double? coordinate,
        double limit,
        string field)
    {
        if (coordinate is double value &&
            (!double.IsFinite(value) || Math.Abs(value) > limit))
        {
            throw new ArgumentException(
                $"Coordinate must be between {-limit} and {limit}.",
                field);
        }
    }
}