namespace BrEconomy.API.Features.Custom.Models;

public record IndicatorResult(
    string Indicator,
    string Description,
    double Value,
    string Date,
    string Source,
    DateTime Timestamp,
    string? Period = null,
    int? DataPointsCount = null,
    List<DataPoint>? DataPoints = null
);

public record DataPoint(
    string Date,
    double Value
);
