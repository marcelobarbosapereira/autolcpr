namespace AutoLCPR.Domain.Common
{
    /// <summary>
    /// Entidade base com id - gerado automaticamente e imutável
    /// </summary>
    public class BaseEntity
    {
        /// <summary>
        /// Identificador único da entidade - gerado automaticamente e não pode ser alterado
        /// </summary>
        public int Id { get; init; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
