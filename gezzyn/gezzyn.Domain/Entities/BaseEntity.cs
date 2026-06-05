namespace gezzyn.Domain.Entities
{
    public class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        public void SetUpdatedAt() => UpdatedAt = DateTime.UtcNow;
        public void SoftDelete()
        {
            IsDeleted = true;
            SetUpdatedAt();
        }
    }
}
