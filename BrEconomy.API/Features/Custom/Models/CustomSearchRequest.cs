using System.ComponentModel.DataAnnotations;

namespace BrEconomy.API.Features.Custom.Models;

/// <summary>
/// Request para busca personalizada de indicadores econômicos usando IA
/// </summary>
public record CustomSearchRequest
{
    /// <summary>
    /// Pergunta em linguagem natural sobre indicadores econômicos
    /// </summary>
    /// <example>Qual a taxa acumulada do CDI nos últimos 5 anos?</example>
    [Required(ErrorMessage = "A pergunta é obrigatória")]
    [MinLength(5, ErrorMessage = "A pergunta deve ter pelo menos 5 caracteres")]
    [MaxLength(500, ErrorMessage = "A pergunta deve ter no máximo 500 caracteres")]
    public required string Query { get; init; }
}
