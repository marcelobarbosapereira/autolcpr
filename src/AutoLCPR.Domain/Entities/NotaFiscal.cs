using AutoLCPR.Domain.Common;

namespace AutoLCPR.Domain.Entities
{
    /// <summary>
    /// Entidade para registro de notas fiscais
    /// </summary>
    public class NotaFiscal : BaseEntity
    {
        public required string ChaveAcesso { get; set; }
        public required string NumeroNotaFiscal { get; set; }
        public decimal ValorNotaFiscal { get; set; }
        public DateTime DataEmissao { get; set; }
        public required string NomeEmitente { get; set; }
        public required string NomeDestinatario { get; set; }
        public required string DocumentoEmitente { get; set; }
        public required string DocumentoDestinatario { get; set; }
        public TipoNota TipoNota { get; set; }

        // Relacionamento com Produtor
        public int ProdutorId { get; set; }
        public virtual Produtor? Produtor { get; set; }
    }
}