using AutoLCPR.Domain.Common;

namespace AutoLCPR.Domain.Entities
{
    /// <summary>
    /// Entidade de chave de acesso importada da SEFAZ por produtor
    /// </summary>
    public class ChaveNFe : BaseEntity
    {
        public int ProdutorId { get; set; }
        public required string ChaveAcesso { get; set; }
        public DateTime DataImportacao { get; set; } = DateTime.Now;

        public virtual Produtor? Produtor { get; set; }
    }
}
