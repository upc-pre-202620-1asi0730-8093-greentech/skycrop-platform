using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Errors;

namespace GreenTech.SkyCrop.Platform.Plots.Domain.Model.Errors;

public static class PlotErrors
{
    public static readonly Error NotFound = Error.NotFound("Plots.NotFound", "The plot was not found.");
    public static Error InvalidData(string message) => Error.Validation("Plots.InvalidData", message);
}
