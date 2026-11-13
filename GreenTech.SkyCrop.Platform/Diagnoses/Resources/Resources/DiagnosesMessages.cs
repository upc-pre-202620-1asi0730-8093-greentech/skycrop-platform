using System.Globalization;
using System.Resources;

namespace GreenTech.SkyCrop.Platform.Diagnoses.Resources;

public static class DiagnosesMessages
{
    private static readonly ResourceManager ResourceManager = new(
        "GreenTech.SkyCrop.Platform.Diagnoses.Resources.DiagnosesMessages",
        typeof(DiagnosesMessages).Assembly);

    public static string? Get(string key) =>
        ResourceManager.GetString(key, CultureInfo.CurrentUICulture);

    public static string RequiredField =>
        Get(nameof(RequiredField))!;

    public static string RangeField =>
        Get(nameof(RangeField))!;

    public static string MinimumItems =>
        Get(nameof(MinimumItems))!;

    public static string MaximumItems =>
        Get(nameof(MaximumItems))!;

    public static string MaximumTextLength =>
        Get(nameof(MaximumTextLength))!;
}