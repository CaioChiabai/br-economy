using BrEconomy.API.Features.Custom.Models;

namespace BrEconomy.API.Features.Custom.Services;

public interface IGroqService
{
    Task<IndicatorInterpretation> InterpretIndicatorRequestAsync(string userQuery);
}
