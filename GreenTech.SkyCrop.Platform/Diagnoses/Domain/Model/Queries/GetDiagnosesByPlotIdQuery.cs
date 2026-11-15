namespace GreenTech.SkyCrop.Platform.Diagnoses.Domain.Model.Queries;

/// <summary>
/// Requests the diagnosis history of a plot.
/// </summary>
public sealed record GetDiagnosesByPlotIdQuery(int PlotId);