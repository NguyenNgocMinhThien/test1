namespace Web_cham_diem.Models
{
    public class RegistrationFormFields
    {
        public int FieldId { get; set; }
        public int CompetitionId { get; set; }

        /// <summary>Nhãn hiển thị trên form, VD: "Link GitHub", "Chủ đề nghiên cứu"</summary>
        public string FieldLabel { get; set; } = string.Empty;

        /// <summary>Text, Textarea, Select, Checkbox, File, Date, Number, Email</summary>
        public string FieldType { get; set; } = "Text";

        public bool IsRequired { get; set; } = false;

        public string? Placeholder { get; set; }

        /// <summary>JSON array of strings cho FieldType = Select, VD: ["Khoa học","Công nghệ"]</summary>
        public string? Options { get; set; }

        public string? HelpText { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Competitions Competition { get; set; } = null!;
        public ICollection<RegistrationFieldValues> FieldValues { get; set; } = new List<RegistrationFieldValues>();
    }
}
