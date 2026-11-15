using System.ComponentModel.DataAnnotations;
using GreenTech.SkyCrop.Platform.Diagnoses.Resources;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest.Resources;

public sealed class GenerateDiagnosisResource
{
    [Range(1, int.MaxValue,
        ErrorMessageResourceType = typeof(DiagnosesMessages),
        ErrorMessageResourceName = nameof(DiagnosesMessages.RangeField))]
    public int PlotId { get; init; }

    [Range(1, int.MaxValue,
        ErrorMessageResourceType = typeof(DiagnosesMessages),
        ErrorMessageResourceName = nameof(DiagnosesMessages.RangeField))]
    public int FlightId { get; init; }

    [Required(
        ErrorMessageResourceType = typeof(DiagnosesMessages),
        ErrorMessageResourceName = nameof(DiagnosesMessages.RequiredField))]
    [MinLength(1,
        ErrorMessageResourceType = typeof(DiagnosesMessages),
        ErrorMessageResourceName = nameof(DiagnosesMessages.MinimumItems))]
    [MaxLength(100,
        ErrorMessageResourceType = typeof(DiagnosesMessages),
        ErrorMessageResourceName = nameof(DiagnosesMessages.MaximumItems))]
    public int[]? ImageIds { get; init; }

    [Required(
        ErrorMessageResourceType = typeof(DiagnosesMessages),
        ErrorMessageResourceName = nameof(DiagnosesMessages.RequiredField))]
    [StringLength(30,
        ErrorMessageResourceType = typeof(DiagnosesMessages),
        ErrorMessageResourceName = nameof(DiagnosesMessages.MaximumTextLength))]
    public string? Scenario { get; init; }
}