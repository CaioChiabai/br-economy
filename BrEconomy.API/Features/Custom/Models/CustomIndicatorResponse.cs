using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BrEconomy.API.Features.Custom.Models;

/// <summary>
/// Resposta para consultas simples (valor atual/mais recente)
/// </summary>
public record SimpleIndicatorResponse(
    /// <summary>Nome do indicador (SELIC, IPCA, CDI, DÓLAR)</summary>
    /// <example>SELIC</example>
    string Indicator,
    
    /// <summary>Valor do indicador</summary>
    /// <example>14.90</example>
    double Value,
    
    /// <summary>Unidade de medida</summary>
    /// <example>% a.a.</example>
    string Unit,
    
    /// <summary>Data de referência</summary>
    /// <example>05/02/2026</example>
    string Date,
    
    /// <summary>Contexto adicional</summary>
    /// <example>SELIC em 05/02/2026</example>
    string Context
);

/// <summary>
/// Resposta para análises de período (acumulado, média, etc.)
/// </summary>
public record PeriodAnalysisResponse(
    /// <summary>Nome do indicador analisado</summary>
    /// <example>CDI</example>
    string Indicator,
    
    /// <summary>Descrição do período analisado</summary>
    /// <example>Análise de 5 anos (61 meses)</example>
    string PeriodDescription,
    
    /// <summary>Resumo estatístico da análise</summary>
    AnalysisSummary Summary,
    
    /// <summary>Informações adicionais sobre os dados</summary>
    /// <example>Baseado em 61 meses de dados oficiais do Banco Central</example>
    string? AdditionalInfo = null
);

/// <summary>
/// Resumo estatístico de uma análise de período
/// </summary>
public record AnalysisSummary(
    /// <summary>Valor principal do resultado (ex: taxa acumulada)</summary>
    /// <example>77.24</example>
    double MainValue,
    
    /// <summary>Descrição do valor principal</summary>
    /// <example>Taxa acumulada em 5 anos</example>
    string MainValueLabel,
    
    /// <summary>Taxa média do período (opcional)</summary>
    double? AverageValue,
    
    /// <summary>Valor inicial do período (opcional)</summary>
    double? InitialValue,
    
    /// <summary>Valor final do período (opcional)</summary>
    double? FinalValue,
    
    /// <summary>Data inicial do período</summary>
    /// <example>01/02/2021</example>
    string? InitialDate,
    
    /// <summary>Data final do período</summary>
    /// <example>01/02/2026</example>
    string? FinalDate,
    
    /// <summary>Quantidade de pontos de dados analisados</summary>
    /// <example>61</example>
    int DataPointsCount,
    
    /// <summary>Exemplo prático do rendimento</summary>
    /// <example>R$ 100 → R$ 177.24</example>
    string? PracticalExample
);

/// <summary>
/// Resposta de erro quando indicador não está disponível
/// </summary>
public record IndicatorNotAvailableResponse(
    string RequestedIndicator,
    string Message,
    List<string> AvailableIndicators,
    List<string> Examples
);
