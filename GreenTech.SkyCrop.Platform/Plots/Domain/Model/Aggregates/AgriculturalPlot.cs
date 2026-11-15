using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Events;
using GreenTech.SkyCrop.Platform.Plots.Domain.Model.Commands;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Aggregates;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Entities;

namespace GreenTech.SkyCrop.Platform.Plots.Domain.Model.Aggregates;

public sealed class AgriculturalPlot : AggregateRoot, IAuditableEntity
{
    public const int TextLength = 120;
    public const int LocationLength = 255;
    public const int ImageUrlLength = 2048;

    private AgriculturalPlot() { }

    public AgriculturalPlot(RegisterPlotCommand command)
    {
        Name = RequiredText(command.Name, nameof(command.Name), TextLength);
        PersonInCharge = RequiredText(command.PersonInCharge, nameof(command.PersonInCharge), TextLength);
        CropType = RequiredText(command.CropType, nameof(command.CropType), TextLength);
        SoilType = RequiredText(command.SoilType, nameof(command.SoilType), TextLength);
        Location = RequiredText(command.Location, nameof(command.Location), LocationLength);
        if (command.SowingDate == default)
            throw new ArgumentException("A sowing date is required.", nameof(command.SowingDate));
        if (command.AreaHectares <= 0 || command.AreaHectares > 999999999999.999999m ||
            decimal.Round(command.AreaHectares, 6) != command.AreaHectares)
            throw new ArgumentException("Area must be positive, fit in decimal(18,6), and have at most six decimal places.",
                nameof(command.AreaHectares));
        if ((command.Latitude is null) != (command.Longitude is null))
            throw new ArgumentException("Latitude and longitude must be supplied together.", nameof(command.Latitude));
        ValidateCoordinate(command.Latitude, 90, nameof(command.Latitude));
        ValidateCoordinate(command.Longitude, 180, nameof(command.Longitude));

        var imageUrl = command.ImageUrl?.Trim() ?? string.Empty;
        if (imageUrl.Length > ImageUrlLength || (imageUrl.Length > 0 &&
            (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
             string.IsNullOrEmpty(uri.Host))))
            throw new ArgumentException("The image URL must be an absolute HTTP or HTTPS URL.",
                nameof(command.ImageUrl));

        SowingDate = command.SowingDate;
        AreaHectares = command.AreaHectares;
        Latitude = command.Latitude;
        Longitude = command.Longitude;
        ImageUrl = imageUrl;

        AddDomainEvent(() => new PlotRegisteredEvent(Id));
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string PersonInCharge { get; private set; } = null!;
    public string CropType { get; private set; } = null!;
    public string SoilType { get; private set; } = null!;
    public DateOnly SowingDate { get; private set; }
    public decimal AreaHectares { get; private set; }
    public string Location { get; private set; } = null!;
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public string ImageUrl { get; private set; } = string.Empty;
    public string Status { get; private set; } = "ACTIVE";
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private static string RequiredText(string? value, string field, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, field);
        var text = value.Trim();
        if (text.Length > maxLength)
            throw new ArgumentException($"Use at most {maxLength} characters.", field);
        return text;
    }

    private static void ValidateCoordinate(double? coordinate, double limit, string field)
    {
        if (coordinate is double value && (!double.IsFinite(value) || Math.Abs(value) > limit))
            throw new ArgumentException($"Use a coordinate between {-limit} and {limit}.", field);
    }
}
