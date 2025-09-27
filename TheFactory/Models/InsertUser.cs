    public class UserInsertModel
    {
        public int UserRoleId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string CellPhoneNo { get; set; }
        public string AlternateCellPhoneNo { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
    }