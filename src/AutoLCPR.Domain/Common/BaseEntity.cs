namespace AutoLCPR.Domain.Common
{
    /// <summary>
    /// Entidade base com id
    /// </summary>
    public class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
