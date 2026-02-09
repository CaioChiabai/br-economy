using System.Text;
using System.Text.Json;
using BrEconomy.API.Features.Custom.Models;

namespace BrEconomy.API.Features.Custom.Services;

public class GroqService : IGroqService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GroqService> _logger;

    public GroqService(IHttpClientFactory httpClientFactory, ILogger<GroqService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IndicatorInterpretation> InterpretIndicatorRequestAsync(string userQuery)
    {
        var client = _httpClientFactory.CreateClient("Groq");

        var systemPrompt = $@"Você é um assistente especializado em indicadores econômicos brasileiros do Banco Central.
Data atual: {DateTime.Now:dd/MM/yyyy}

Indicadores disponíveis: SELIC, IPCA, CDI, DÓLAR

Retorne JSON válido (sem markdown):
{{
  ""indicator"": ""SELIC|IPCA|CDI|DÓLAR"",
  ""bcbCode"": CÓDIGO,
  ""period"": ""last|ytd|12m|custom"",
  ""description"": ""Descrição"",
  ""isValid"": true|false,
  ""suggestion"": ""mensagem se inválido"",
  ""startDate"": ""DD/MM/YYYY"" (se period=custom),
  ""endDate"": ""DD/MM/YYYY"" (se period=custom),
  ""requestedPeriodYears"": número (se solicitou X anos),
  ""aggregationType"": ""accumulated|average|last""
}}

Códigos BCB:
- SELIC: 432
- IPCA: 433  
- CDI: 4392
- DÓLAR: 1

Períodos:
- last: valor atual
- ytd: acumulado no ano
- 12m: últimos 12 meses
- custom: período específico (calcule startDate/endDate baseado em {DateTime.Now:dd/MM/yyyy})

Se pedir Bitcoin, ações, ouro → isValid=false";

        var requestBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userQuery }
            },
            temperature = 0.1,
            max_tokens = 300
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Enviando query para Groq: {Query}", userQuery);

        try
        {
            var response = await client.PostAsync("/openai/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var messageContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            _logger.LogInformation("Resposta Groq: {Response}", messageContent);

            // Limpar markdown se a IA retornou com ```json
            var cleanedContent = messageContent!
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            var interpretation = JsonSerializer.Deserialize<IndicatorInterpretation>(
                cleanedContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (interpretation != null)
            {
                // Validar campos obrigatórios
                if (string.IsNullOrEmpty(interpretation.Indicator))
                {
                    _logger.LogWarning("IA retornou interpretação inválida sem indicador");
                    return CreateInvalidResponse("Não consegui identificar qual indicador você está buscando");
                }

                return interpretation;
            }

            _logger.LogWarning("Falha ao desserializar resposta da IA");
            return CreateInvalidResponse("Não consegui processar sua solicitação");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao parsear JSON da IA. Resposta recebida pode estar malformada");
            return CreateInvalidResponse("Erro ao interpretar a resposta. Tente reformular sua pergunta");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao comunicar com serviço de IA");
            return CreateInvalidResponse("Erro temporário ao processar sua solicitação");
        }
    }

    private IndicatorInterpretation CreateInvalidResponse(string message)
    {
        return new IndicatorInterpretation(
            Indicator: "UNKNOWN",
            BcbCode: 0,
            Period: "last",
            Description: message,
            IsValid: false,
            Suggestion: $"{message}. Este sistema fornece apenas indicadores do Banco Central: SELIC, IPCA, CDI e DÓLAR."
        );
    }
}
