namespace AutoLCPR.Domain.Common;

/// <summary>
/// Classe base para todas as entidades de domínio
/// </summary>
public abstract class Entity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    protected Entity()
    {
        CreatedAt = DateTime.UtcNow;
    }
}
