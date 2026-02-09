using BrEconomy.API.Features.Custom.Models;

namespace BrEconomy.API.Features.Custom.Services;

public interface ICustomIndicatorService
{
    Task<IndicatorResult> FetchIndicatorAsync(IndicatorInterpretation interpretation);
}
