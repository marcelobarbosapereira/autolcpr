using AutoLCPR.Domain.Common;

namespace AutoLCPR.Domain.Entities;

public class MovimentacaoRebanho : BaseEntity
{
    public DateTime Data { get; set; }
    public required string TipoMovimentacao { get; set; }
    public string? Descricao { get; set; }
    public int Quantidade { get; set; }
    public int ProdutorId { get; set; }
    public virtual Produtor? Produtor { get; set; }
}
