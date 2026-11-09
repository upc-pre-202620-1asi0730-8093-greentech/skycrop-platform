namespace GreenTech.SkyCrop.Platform.Drones.Domain.Model.ValueObjects;

internal static class DroneValidation
{
    public static void PositiveMeasurement(decimal value, string field)
    {
        if (value <= 0 || value > 999999999999.999999m || decimal.Round(value, 6) != value)
            throw new ArgumentException($"{field} must be positive, at most 999999999999.999999, and have at most six decimal places.");
    }

    public static string RequiredText(string? value, string field, int maximumLength = 120)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, field);
        var result = value.Trim();
        if (result.Length > maximumLength) throw new ArgumentException($"{field} cannot exceed {maximumLength} characters.");
        return result;
    }

    public static string HttpUrl(string? value, bool optional = true)
    {
        if (optional && string.IsNullOrWhiteSpace(value)) return string.Empty;
        var result = RequiredText(value, "imageUrl", 2048);
        if (!Uri.TryCreate(result, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || string.IsNullOrEmpty(uri.Host))
            throw new ArgumentException("The image URL must be an absolute HTTP or HTTPS URL.");
        return result;
    }

    public static T Status<T>(string value) where T : struct, Enum
    {
        if (!Enum.GetNames<T>().Contains(value) || !Enum.TryParse<T>(value, out var result))
            throw new ArgumentException($"Invalid {typeof(T).Name} value.");
        return result;
    }
}
