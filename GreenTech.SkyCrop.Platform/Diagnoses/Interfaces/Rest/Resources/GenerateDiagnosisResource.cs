using System.ComponentModel.DataAnnotations;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest.Resources;

/// <summary>
/// Requests simulated generation. Results are calculated by the backend.
/// </summary>
public sealed class GenerateDiagnosisResource
{
    [Range(1, int.MaxValue)]
    public int PlotId { get; init; }

    [Range(1, int.MaxValue)]
    public int FlightId { get; init; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public int[]? ImageIds { get; init; }

    [Required]
    [StringLength(30)]
    public string? Scenario { get; init; }
}