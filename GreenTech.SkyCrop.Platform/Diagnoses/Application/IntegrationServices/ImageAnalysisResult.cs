using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Entities;
using GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.ValueObjects;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Application.IntegrationServices;

public sealed record ImageAnalysisResult(
    string Observations,
    Ndvi AverageNdvi,
    IReadOnlyCollection<Anomaly> Anomalies);