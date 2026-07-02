namespace Web_cham_diem.Models
{
    public class RegistrationFieldValues
    {
        public int ValueId { get; set; }
        public int RegistrationId { get; set; }
        public int FieldId { get; set; }

        /// <summary>Giá trị thí sinh nhập: text, số, ngày, "true/false" cho Checkbox, URL cho File</summary>
        public string? Value { get; set; }

        /// <summary>Tên file gốc (chỉ dùng khi FieldType = File)</summary>
        public string? FileName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Registrations Registration { get; set; } = null!;
        public RegistrationFormFields Field { get; set; } = null!;
    }
}
