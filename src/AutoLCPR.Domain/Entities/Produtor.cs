using AutoLCPR.Domain.Common;

namespace AutoLCPR.Domain.Entities
{
    /// <summary>
    /// Entidade para registro de produtores rurais
    /// </summary>
    public class Produtor : BaseEntity
    {
        public required string Nome { get; set; }

        // Relacionamentos
        public virtual ICollection<Rebanho> Rebanhos { get; set; } = new List<Rebanho>();
        public virtual ICollection<NotaFiscal> NotasFiscais { get; set; } = new List<NotaFiscal>();
    }
}