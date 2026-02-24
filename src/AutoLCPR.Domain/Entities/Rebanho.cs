using AutoLCPR.Domain.Common;

namespace AutoLCPR.Domain.Entities
{
    /// <summary>
    /// Entidade para registro de rebanhos
    /// </summary>
    public class Rebanho : BaseEntity
    {
        public required string IdRebanho { get; set; }
        public required string NomeRebanho { get; set; }
        public int Mortes { get; set; }
        public int Nascimentos { get; set; }
        public int Entradas { get; set; }
        public int Saidas { get; set; }

        // Relacionamento com Produtor
        public int ProdutorId { get; set; }
        public virtual Produtor? Produtor { get; set; }
    }
}