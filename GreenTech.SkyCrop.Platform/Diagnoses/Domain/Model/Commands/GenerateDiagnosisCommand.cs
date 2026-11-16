namespace GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Commands;

// Standalone simulated analysis; no plot, flight or image records are required.
public sealed record GenerateDiagnosisCommand(string Scenario);
