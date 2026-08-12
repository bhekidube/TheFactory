namespace TheFactory.Models
{
    public class Subject
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public ICollection<Mark> Marks { get; set; } = new List<Mark>();
    }
}