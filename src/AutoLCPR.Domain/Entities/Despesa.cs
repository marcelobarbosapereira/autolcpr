using AutoLCPR.Domain.Common;

namespace AutoLCPR.Domain.Entities
{
    /// <summary>
    /// Entidade para registro de despesas
    /// </summary>
    public class Despesa : BaseEntity
    {
        public required string Descricao { get; set; }
        public required string Documento { get; set; }
        public required string Fornecedor { get; set; }
        public decimal Valor { get; set; }
        public DateTime DataVencimento { get; set; }
        public DateTime? DataPagamento { get; set; }
        public string Status { get; set; } = "Pendente"; // Pendente, Pago, Cancelado
    }
}
