namespace AutoLCPR.Application.Relatorios;

public sealed class RelatorioAnualDto
{
    public required string NomeProdutor { get; init; }
    public required IReadOnlyList<PropriedadeRelatorioDto> Propriedades { get; init; }
    public int AnoExercicio { get; init; }
    public int AnoBase { get; init; }
    public DateTime DataInicio { get; init; }
    public DateTime DataFim { get; init; }
    public required IReadOnlyList<ResumoMensalDto> ResumoMensal { get; init; }
    public decimal TotalReceitas { get; init; }
    public decimal TotalDespesas { get; init; }
    public decimal MargemAnual { get; init; }
    public required IReadOnlyList<ResumoRebanhoDto> ResumoRebanho { get; init; }
}

public sealed class ResumoMensalDto
{
    public int Mes { get; init; }
    public required string NomeMes { get; init; }
    public decimal Receita { get; init; }
    public decimal Despesa { get; init; }
}

public sealed class ResumoRebanhoDto
{
    public required string TipoMovimentacao { get; init; }
    public int Quantidade { get; init; }
}

public sealed class PropriedadeRelatorioDto
{
    public required string NomePropriedade { get; init; }
    public required string InscricaoPropriedade { get; init; }
    public int TotalNascimentos { get; init; }
    public int TotalCompras { get; init; }
    public int TotalVendas { get; init; }
    public int TotalObitos { get; init; }
}
