namespace BrEconomy.API.Features.Custom.Models;

public record IndicatorInterpretation(
    string Indicator,
    int BcbCode,
    string Period,
    string Description,
    bool IsValid,
    string? Suggestion = null,
    string? StartDate = null,
    string? EndDate = null,
    int? RequestedPeriodYears = null,
    string? AggregationType = null
);
