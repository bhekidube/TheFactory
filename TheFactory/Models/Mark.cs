namespace TheFactory.Models
{
    public class Mark
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int LearnerId { get; set; }
        public int SubjectId { get; set; }
        public int Score { get; set; }
        public string? TeacherComments { get; set; }
        public Subject? Subject { get; set; }
    }
}