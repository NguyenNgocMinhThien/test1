# HƯỚ NG DẪN PHÁT TRIỂN HỆ THỐNG

## 📋 Tóm tắt

Hệ thống quản lý cuộc thi sinh viên đã được thiết kế và triển khai với:
- ✅ Database Schema hoàn chỉnh (chuẩn hóa 3NF)
- ✅ 10 Entities chính
- ✅ Các Foreign Keys và Relationships
- ✅ Migration & Database Initialization
- ✅ Seed data cho Roles

---

## 🗄️ Database Schema

### Bảng Roles
```sql
Roles
├── RoleId (PK)
├── RoleName: 'Admin', 'Student', 'Organizer', 'Judge', 'Lecturer'
├── Description
└── CreatedAt, UpdatedAt
```

### Bảng Users
```sql
Users
├── UserId (PK)
├── FullName
├── Email (UNIQUE)
├── PhoneNumber
├── StudentId (MSSV)
├── RoleId (FK → Roles)
├── PasswordHash
├── IsActive
└── CreatedAt, UpdatedAt, LastLogin
```

### Bảng Competitions
```sql
Competitions
├── CompetitionId (PK)
├── CompetitionName
├── Category
├── StartDate, EndDate
├── RegistrationDeadline
├── SubmissionDeadline
├── MaxParticipants, MaxTeamSize
├── MaxScore
├── Status: 'Draft', 'Active', 'Closed', 'Completed'
├── IsTeamBased
├── Prize
└── CreatedAt, UpdatedAt
```

### Bảng Registrations
```sql
Registrations
├── RegistrationId (PK)
├── UserId (FK)
├── CompetitionId (FK)
├── TeamId (FK, nullable)
├── RegistrationType: 'Individual', 'Team'
├── Status: 'Pending', 'Approved', 'Rejected', 'Withdrawn'
├── SubmissionDocument
└── RegistrationDate, ApprovalDate
```

### Bảng Teams
```sql
Teams
├── TeamId (PK)
├── TeamName
├── CompetitionId (FK)
├── LeaderId (FK → Users)
├── Status: 'Active', 'Disbanded'
└── CreatedAt, UpdatedAt
```

### Bảng Submissions
```sql
Submissions
├── SubmissionId (PK)
├── CompetitionId (FK)
├── RegistrationId (FK, nullable)
├── TeamId (FK, nullable)
├── Round: 1, 2, 3...
├── Title
├── FileUrl, VideoUrl, ProjectLink
├── Status: 'Draft', 'Submitted', 'Under Review', 'Evaluated'
└── SubmissionDate, UpdatedAt
```

### Bảng Judges
```sql
Judges
├── JudgeId (PK)
├── UserId (FK)
├── CompetitionId (FK)
├── Expertise
├── Priority: 0 (cao), 1 (trung bình), 2 (thấp)
├── Status: 'Active', 'Inactive'
└── AssignedDate, UpdatedAt
```

### Bảng ScoringCriteria
```sql
ScoringCriteria
├── CriteriaId (PK)
├── CompetitionId (FK)
├── CriteriaName: 'Ý tưởng', 'Kỹ thuật', 'Thuyết trình'...
├── MaxScore: Điểm tối đa (18.2 decimal)
├── Weight: Trọng số (18.2 decimal)
├── Order
└── CreatedAt, UpdatedAt
```

### Bảng Scores
```sql
Scores
├── ScoreId (PK)
├── SubmissionId (FK)
├── JudgeId (FK)
├── CriteriaId (FK)
├── Score: Điểm chấm (18.2 decimal)
├── Comment
└── ScoredDate, UpdatedAt
```

### Bảng Notifications
```sql
Notifications
├── NotificationId (PK)
├── UserId (FK)
├── Title
├── Message
├── Type: 'Info', 'Warning', 'Success', 'Error'
├── RelatedEntity & RelatedEntityId
├── IsRead
└── CreatedAt, ReadAt
```

---

## 🚀 Các Bước Tiếp Theo

### 1. Tạo Repositories (Data Access Layer)
```csharp
// Interfaces
IGenericRepository<T>
ICompetitionRepository
IRegistrationRepository
ISubmissionRepository
IScoreRepository

// Implementation
GenericRepository<T>
CompetitionRepository
...
```

### 2. Tạo Services (Business Logic Layer)
```csharp
ICompetitionService
IRegistrationService
ISubmissionService
IJudgingService
INotificationService
IReportService
```

### 3. Tạo Controllers (API Layer)
```csharp
CompetitionsController
RegistrationsController
SubmissionsController
JudgesController
ScoresController
NotificationsController
```

### 4. Implement Authentication & Authorization
```csharp
// JWT Token
AuthenticationService
JwtTokenHandler

// Authorization Policies
[Authorize(Roles = "Admin")]
[Authorize(Roles = "Organizer")]
```

### 5. Validation & Error Handling
```csharp
// FluentValidation
CompetitionValidator
RegistrationValidator
...

// Custom Exceptions
InvalidRegistrationException
ScoreCalculationException
```

### 6. Implement Business Logic Examples

#### Đăng ký tham dự
```csharp
async Task RegisterAsync(int userId, int competitionId, bool isTeam)
{
    // Validate competition status, deadline
    // Check user eligibility
    // Create registration
    // Send notification
}
```

#### Tính điểm
```csharp
async Task<decimal> CalculateFinalScoreAsync(int submissionId)
{
    // Get all scores for submission
    // Group by criteria
    // Calculate average per criteria
    // Apply weights
    // Return final score
}
```

#### Công bố kết quả
```csharp
async Task PublishResultsAsync(int competitionId)
{
    // Calculate rankings
    // Update notification for all participants
    // Send emails
    // Update competition status
}
```

---

## 📌 Relationships Diagram

```
┌─────────────┐
│   Roles     │ (1)
└──────┬──────┘
       │ 1:n
       │
┌──────▼──────┐
│   Users     │ (1)
└──────┬──────┘
       │ 1:n
       ├─────────────────────────────┐
       │                             │
┌──────▼─────────┐        ┌─────────▼──────┐
│ Registrations  │        │  Judges        │
└──────┬─────────┘        └─────────┬──────┘
       │                            │ 1:n
       │ n:1                        │
       │            ┌───────────────▼──────┐
       │            │     Scores           │
       │            └──────┬───────────────┘
       │                   │ 1:n
┌──────▼──────┐   ┌────────▼───────────┐
│Competitions │   │ ScoringCriteria    │
└──────┬──────┘   └────────────────────┘
       │ 1:n
       ├─────────────┐
       │             │
┌──────▼──┐  ┌──────▼─────┐
│  Teams  │  │Submissions │
└─────────┘  └────────────┘
       │ 1:n
       │
┌──────▼──────┐
│Notification │
└─────────────┘
```

---

## 🔧 Commands Utility

```bash
# Tạo migration mới
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Revert database
dotnet ef database update PreviousMigration

# Drop database
dotnet ef database drop

# List migrations
dotnet ef migrations list

# Build project
dotnet build

# Run project
dotnet run

# Run tests
dotnet test
```

---

## 📝 Seed Data

Dữ liệu khởi tạo của Roles đã được tự động seed khi migrate:

| RoleId | RoleName | Description |
|--------|----------|-------------|
| 1 | Admin | Quản trị viên hệ thống |
| 2 | Student | Sinh viên tham dự cuộc thi |
| 3 | Organizer | Ban tổ chức cuộc thi |
| 4 | Judge | Giám khảo |
| 5 | Lecturer | Giảng viên hướng dẫn |

---

## ⚠️ Important Notes

1. **Cascade Delete Policy**: Được cấu hình cẩn thận để tránh data loss
   - `Users.Registrations` → CASCADE
   - `Competition.Submissions` → NO ACTION
   - `Team.Submissions` → NO ACTION
   
2. **Decimal Precision**: Tất cả scores sử dụng `decimal(18,2)` = 18 digits, 2 decimals

3. **Soft Delete**: Nếu cần implement soft delete trong tương lai:
   - Thêm `IsDeleted` boolean field
   - Query luôn filter `!IsDeleted`

4. **Indexes**: Được tạo tự động trên FK và Unique columns

5. **Unique Constraint**: Email của Users phải unique

---

## 🎯 Success Criteria

✅ Database được tạo thành công  
✅ Tất cả constraints & relationships đúng  
✅ Seed data được insert  
✅ Migration có thể revert được  
✅ Build không có lỗi  

---

## 📞 Support

Mọi thắc mắc liên quan đến database schema hoặc design, vui lòng tham khảo file `SYSTEM_DESIGN.md`
