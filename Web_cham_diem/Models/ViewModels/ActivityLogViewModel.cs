namespace Web_cham_diem.Models.ViewModels
{
    public class ActivityLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; } = "Hệ thống";
        public string UserEmail { get; set; } = "";
        public string RoleName { get; set; } = "";

        // CREATE, UPDATE, DELETE, LOGIN
        public string ActionType { get; set; } = "UPDATE";

        public string Module { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
