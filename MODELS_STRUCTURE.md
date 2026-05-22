# 📊 KIẾN TRÚC MODELS - TÓM TẮT

## 1️⃣ Roles (Vai Trò)

```csharp
public class Roles
{
    public int RoleId { get; set; }
    public string RoleName { get; set; }           // Admin, Student, Organizer, Judge, Lecturer
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public ICollection<Users> Users { get; set; }
}
```

**Dữ liệu ban đầu:**
- 1: Admin - Quản trị viên hệ thống
- 2: Student - Sinh viên tham dự cuộc thi
- 3: Organizer - Ban tổ chức cuộc thi
- 4: Judge - Giám khảo
- 5: Lecturer - Giảng viên hướng dẫn

---

## 2️⃣ Users (Người Dùng)

```csharp
public class Users
{
    public int UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }              // UNIQUE
    public string PhoneNumber { get; set; }
    public string PasswordHash { get; set; }
    public string? StudentId { get; set; }         // MSSV
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    
    // Foreign Keys
    public Roles Role { get; set; }
    
    // Navigation
    public ICollection<Registrations> Registrations { get; set; }
    public ICollection<Judges> JudgeAssignments { get; set; }
    public ICollection<Notifications> Notifications { get; set; }
}
```

---

## 3️⃣ Competitions (Cuộc Thi)

```csharp
public class Competitions
{
    public int CompetitionId { get; set; }
    public string CompetitionName { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }           // Lĩnh vực thi
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public DateTime SubmissionDeadline { get; set; }
    public int MaxParticipants { get; set; }
    public int MaxTeamSize { get; set; }
    public decimal MaxScore { get; set; } = 100
    public string Status { get; set; }              // Draft, Active, Closed, Completed
    public bool IsTeamBased { get; set; } = false
    public string? Rules { get; set; }
    public string? Prize { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public ICollection<Registrations> Registrations { get; set; }
    public ICollection<Teams> Teams { get; set; }
    public ICollection<Submissions> Submissions { get; set; }
    public ICollection<ScoringCriteria> ScoringCriteria { get; set; }
}
```

---

## 4️⃣ Registrations (Đơn Đăng Ký)

```csharp
public class Registrations
{
    public int RegistrationId { get; set; }
    public int UserId { get; set; }
    public int CompetitionId { get; set; }
    public int? TeamId { get; set; }
    public string RegistrationType { get; set; }   // Individual, Team
    public string Status { get; set; }              // Pending, Approved, Rejected, Withdrawn
    public string? SubmissionDocument { get; set; }
    public string? Notes { get; set; }
    public DateTime RegistrationDate { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Foreign Keys
    public Users User { get; set; }
    public Competitions Competition { get; set; }
    public Teams? Team { get; set; }
}
```

---

## 5️⃣ Teams (Đội Thi)

```csharp
public class Teams
{
    public int TeamId { get; set; }
    public string TeamName { get; set; }
    public int CompetitionId { get; set; }
    public int LeaderId { get; set; }              // UserId trưởng đội
    public string? Description { get; set; }
    public string Status { get; set; }             // Active, Disbanded
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Foreign Keys
    public Competitions Competition { get; set; }
    public Users Leader { get; set; }
    
    // Navigation
    public ICollection<Registrations> Registrations { get; set; }
    public ICollection<Submissions> Submissions { get; set; }
}
```

---

## 6️⃣ Submissions (Bài Dự Thi)

```csharp
public class Submissions
{
    public int SubmissionId { get; set; }
    public int CompetitionId { get; set; }
    public int? RegistrationId { get; set; }       // Cho cá nhân
    public int? TeamId { get; set; }               // Cho đội
    public int Round { get; set; } = 1             // Vòng thi
    public string Title { get; set; }
    public string? Description { get; set; }
    public string? FileUrl { get; set; }           // Link file
    public string? VideoUrl { get; set; }          // Link video
    public string? ProjectLink { get; set; }       // Link sản phẩm
    public string Status { get; set; }             // Draft, Submitted, Under Review, Evaluated
    public DateTime SubmissionDate { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Foreign Keys
    public Competitions Competition { get; set; }
    public Registrations? Registration { get; set; }
    public Teams? Team { get; set; }
    
    // Navigation
    public ICollection<Scores> Scores { get; set; }
}
```

---

## 7️⃣ Judges (Giám Khảo)

```csharp
public class Judges
{
    public int JudgeId { get; set; }
    public int UserId { get; set; }
    public int CompetitionId { get; set; }
    public string Expertise { get; set; }          // Lĩnh vực chuyên môn
    public int Priority { get; set; } = 0          // 0=cao, 1=trung bình, 2=thấp
    public string Status { get; set; }             // Active, Inactive
    public DateTime AssignedDate { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Foreign Keys
    public Users User { get; set; }
    public Competitions Competition { get; set; }
    
    // Navigation
    public ICollection<Scores> Scores { get; set; }
}
```

---

## 8️⃣ ScoringCriteria (Tiêu Chí Chấm Điểm)

```csharp
public class ScoringCriteria
{
    public int CriteriaId { get; set; }
    public int CompetitionId { get; set; }
    public string CriteriaName { get; set; }       // Ý tưởng, Kỹ thuật, Thuyết trình...
    public string? Description { get; set; }
    public decimal MaxScore { get; set; } = 10
    public decimal Weight { get; set; } = 1.0m    // Trọng số
    public int Order { get; set; } = 0
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Foreign Keys
    public Competitions Competition { get; set; }
    
    // Navigation
    public ICollection<Scores> Scores { get; set; }
}
```

---

## 9️⃣ Scores (Điểm Chấm)

```csharp
public class Scores
{
    public int ScoreId { get; set; }
    public int SubmissionId { get; set; }
    public int JudgeId { get; set; }
    public int CriteriaId { get; set; }
    public decimal Score { get; set; }             // Điểm chấm
    public string? Comment { get; set; }           // Nhận xét
    public DateTime ScoredDate { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Foreign Keys
    public Submissions Submission { get; set; }
    public Judges Judge { get; set; }
    public ScoringCriteria Criteria { get; set; }
}
```

**Ví dụ:**
```
Submission: "AI Chatbot" (ID: 1)
Judge: "Dr. Lê Văn A" (ID: 1)
Criteria: "Ý tưởng" (ID: 1)
Score: 8.5/10

Submission: "AI Chatbot" (ID: 1)
Judge: "Dr. Nguyễn Thị B" (ID: 2)
Criteria: "Ý tưởng" (ID: 1)
Score: 9.0/10

→ AvgScore = (8.5 + 9.0) / 2 = 8.75
```

---

## 🔟 Notifications (Thông Báo)

```csharp
public class Notifications
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string Type { get; set; }               // Info, Warning, Success, Error
    public string? RelatedEntity { get; set; }     // Competition, Registration, Submission, Score
    public int? RelatedEntityId { get; set; }
    public bool IsRead { get; set; } = false
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    
    // Foreign Keys
    public Users User { get; set; }
}
```

---

## 🔗 Tất Cả Relationships

```
Roles (1) ←→ (n) Users
     ↓
Users (1) ←→ (n) Registrations
Users (1) ←→ (n) Judges
Users (1) ←→ (n) Notifications
     ↓
Competitions (1) ←→ (n) Registrations
Competitions (1) ←→ (n) Teams
Competitions (1) ←→ (n) Submissions
Competitions (1) ←→ (n) ScoringCriteria
     ↓
Teams (1) ←→ (n) Registrations
Teams (1) ←→ (n) Submissions
     ↓
Registrations (1) ←→ (n) Submissions
Judges (1) ←→ (n) Scores
ScoringCriteria (1) ←→ (n) Scores
Submissions (1) ←→ (n) Scores
```

---

## 📌 Delete Behaviors

```
CASCADE (Xóa cha → Xóa con):
- Users → Registrations
- Users → Judges
- Users → Notifications
- Judges → Scores
- Submissions → Scores

SET NULL (Xóa cha → NULL con):
- Registrations → Teams
- Submissions → Registrations
- Submissions → Teams

RESTRICT (Không được xóa nếu con tồn tại):
- Roles → Users
- Users → Teams (Leader)
- Judges → Scores
- ScoringCriteria → Scores

NO ACTION (Prevent delete nếu FK exist):
- Competitions → Registrations
- Competitions → Teams
- Competitions → Submissions
```

---

## 💾 Data Types

| Field | Type | Length |
|-------|------|--------|
| RoleId, UserId, CompetitionId... | int | - |
| Email | nvarchar | 100 |
| FullName, TeamName | nvarchar | 200 |
| Description, Notes... | nvarchar | max |
| Score, MaxScore, Weight | decimal | (18,2) |
| Status, Type | nvarchar | max |
| Dates | datetime2 | - |
| IsActive, IsRead | bit | - |

---

## 🎯 Tóm Tắt

✅ **10 Entities** hoàn chỉnh  
✅ **15+ Relationships** được định nghĩa rõ ràng  
✅ **30+ Properties** với đúng type  
✅ **Foreign Keys & Constraints** cấu hình đúng  
✅ **Seed Data** cho Roles  
✅ **Database** được khởi tạo  
✅ **Build** thành công không lỗi  

Sẵn sàng để phát triển **Repositories**, **Services**, và **Controllers** 🚀
