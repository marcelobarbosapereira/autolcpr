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
        public TipoNota TipoNota { get; set; } = TipoNota.Saida;
        public bool XmlBaixado { get; set; }
        public string? StatusDownload { get; set; }
        public DateTime? DataDownload { get; set; }
        public string? CaminhoXml { get; set; }

        // Relacionamento com Produtor
        public int ProdutorId { get; set; }
        public virtual Produtor? Produtor { get; set; }
        public virtual ICollection<Lancamento> Lancamentos { get; set; } = new List<Lancamento>();
    }
}