using AutoLCPR.Domain.Common;

namespace AutoLCPR.Domain.Entities
{
    /// <summary>
    /// Entidade para registro de notas fiscais
    /// </summary>
    public class NotaFiscal : BaseEntity
    {
        public string? ChaveAcesso { get; set; }
        public required DateTime DataEmissao { get; set; }
        public required string NumeroDaNota { get; set; }
        public required decimal ValorNotaFiscal { get; set; }
        public required string Origem { get; set; }
        public required string Destino { get; set; }
        public required string Descricao { get; set; }
        public string? NaturezaOperacao { get; set; }
        public string? Cfops { get; set; }
        public string? ItensDescricao { get; set; }
        public TipoNota TipoNota { get; set; } = TipoNota.Saida;

        // Relacionamento com Produtor
        public int ProdutorId { get; set; }
        public virtual Produtor? Produtor { get; set; }
    }
}