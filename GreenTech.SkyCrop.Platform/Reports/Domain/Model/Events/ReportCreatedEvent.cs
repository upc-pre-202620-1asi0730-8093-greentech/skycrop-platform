using GreenTech.SkyCrop.Platform.Reports.Domain.Model.ValueObjects;
using GreenTech.SkyCrop.Platform.Shared.Domain.Model.Events;

namespace GreenTech.SkyCrop.Platform.Reports.Domain.Model.Events;

public record ReportCreatedEvent(ReportId ReportId) : IEvent;