using System.ComponentModel.DataAnnotations;
using GreenTech.SkyCrop.Platform.Diagnoses.Resources;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Interfaces.Rest.Resources;

public sealed class GenerateDiagnosisResource
{
    [Required(
        ErrorMessageResourceType = typeof(DiagnosesMessages),
        ErrorMessageResourceName = nameof(DiagnosesMessages.RequiredField))]
    [StringLength(30,
        ErrorMessageResourceType = typeof(DiagnosesMessages),
        ErrorMessageResourceName = nameof(DiagnosesMessages.MaximumTextLength))]
    public string? Scenario { get; init; }
}