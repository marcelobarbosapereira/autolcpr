using AutoLCPR.Domain.Common;

namespace AutoLCPR.Domain.Entities;

public class Lancamento : BaseEntity
{
    public DateTime Data { get; set; }
    public TipoLancamento Tipo { get; set; }
    public required string ClienteFornecedor { get; set; }
    public required string Descricao { get; set; }
    public required string Situacao { get; set; }
    public decimal Valor { get; set; }
    public DateTime Vencimento { get; set; }
    public int ProdutorId { get; set; }
    public int? NotaFiscalId { get; set; }
    public virtual Produtor? Produtor { get; set; }
    public virtual NotaFiscal? NotaFiscal { get; set; }
}
