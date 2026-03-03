namespace AutoLCPR.Application.Relatorios;

public sealed class RelatorioRebanhoDto
{
    public required string NomeProdutor { get; init; }
    public int AnoExercicio { get; init; }
    public int AnoBase { get; init; }
    public DateTime DataInicio { get; init; }
    public DateTime DataFim { get; init; }
    public required DateTime DataGeracao { get; init; }
    public required IReadOnlyList<PropriedadeRebanhoDto> Propriedades { get; init; }
    public required ResumoRebanhoAnualDto Resumo { get; init; }
}

public sealed class ResumoRebanhoAnualDto
{
    public int TotalNascimentos { get; init; }
    public int TotalCompras { get; init; }
    public int TotalVendas { get; init; }
    public int TotalObitos { get; init; }
    public int SaldoRebanhoAno { get; init; }
}

public sealed class PropriedadeRebanhoDto
{
    public required string NomePropriedade { get; init; }
    public required string InscricaoPropriedade { get; init; }
    public int TotalNascimentos { get; init; }
    public int TotalCompras { get; init; }
    public int TotalVendas { get; init; }
    public int TotalObitos { get; init; }
}
