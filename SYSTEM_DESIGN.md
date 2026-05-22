# THIẾT KẾ HỆ THỐNG QUẢN LÝ CUỘC THI SINH VIÊN

## 1. PHÂN TÍCH KIẾN TRÚC

### 1.1 Kiến trúc tổng quan

```
┌─────────────────────────────────────────────────────┐
│         WEB API - QUẢN LÝ CUỘC THI                 │
│         (ASP.NET Core + Entity Framework Core)     │
└─────────────────────────────────────────────────────┘
                           ↓
     ┌─────────────────────────────────────────┐
     │      DATABASE - SQL Server / SQLite     │
     └─────────────────────────────────────────┘
```

### 1.2 3 Module chính

#### Module 1: Quản lý Cuộc thi & Đăng ký
- Tạo/Chỉnh sửa/Xóa cuộc thi
- Quản lý hồ sơ đăng ký
- Xét duyệt đăng ký
- Phân quyền người dùng

#### Module 2: Quản lý Bài dự thi & Chấm điểm
- Nộp bài trực tuyến
- Upload tài liệu/video/link
- Phân công giám khảo
- Chấm điểm theo tiêu chí
- Tính điểm trung bình

#### Module 3: Công bố Kết quả & Báo cáo
- Tổng hợp điểm
- Xếp hạng thí sinh
- Công bố kết quả
- Dashboard thống kê
- Xuất PDF/Excel
- Gửi Notification/Email

---

## 2. THIẾT KẾ CƠ SỞ DỮ LIỆU

### 2.1 Các Entities & Relationships

```
ROLES (1)
  ├── RoleId (PK)
  ├── RoleName: Admin, Student, Organizer, Judge, Lecturer
  └── 1 ←→ n USERS

USERS (n)
  ├── UserId (PK)
  ├── Email (Unique)
  ├── FullName
  ├── PhoneNumber
  ├── StudentId (MSSV)
  ├── RoleId (FK → ROLES)
  ├── IsActive
  ├── CreatedAt, UpdatedAt, LastLogin
  └── 1 ←→ n REGISTRATIONS
      1 ←→ n JUDGES (JudgeAssignments)
      1 ←→ n NOTIFICATIONS

COMPETITIONS (n)
  ├── CompetitionId (PK)
  ├── CompetitionName
  ├── Category
  ├── StartDate, EndDate
  ├── RegistrationDeadline
  ├── SubmissionDeadline
  ├── MaxParticipants, MaxTeamSize
  ├── MaxScore
  ├── Status: Draft, Active, Closed, Completed
  ├── IsTeamBased
  ├── Prize
  └── 1 ←→ n REGISTRATIONS
      1 ←→ n TEAMS
      1 ←→ n SUBMISSIONS
      1 ←→ n SCORING_CRITERIA

REGISTRATIONS (n)
  ├── RegistrationId (PK)
  ├── UserId (FK → USERS)
  ├── CompetitionId (FK → COMPETITIONS)
  ├── TeamId (FK → TEAMS) [Nullable]
  ├── RegistrationType: Individual, Team
  ├── Status: Pending, Approved, Rejected, Withdrawn
  ├── SubmissionDocument
  ├── RegistrationDate
  └── ApprovalDate, UpdatedAt

TEAMS (n)
  ├── TeamId (PK)
  ├── TeamName
  ├── CompetitionId (FK → COMPETITIONS)
  ├── LeaderId (FK → USERS)
  ├── Status: Active, Disbanded
  ├── CreatedAt, UpdatedAt
  └── 1 ←→ n REGISTRATIONS
      1 ←→ n SUBMISSIONS

SUBMISSIONS (n)
  ├── SubmissionId (PK)
  ├── CompetitionId (FK → COMPETITIONS)
  ├── RegistrationId (FK → REGISTRATIONS) [Nullable]
  ├── TeamId (FK → TEAMS) [Nullable]
  ├── Round: 1, 2, 3... (Vòng thi)
  ├── Title
  ├── FileUrl, VideoUrl, ProjectLink
  ├── Status: Draft, Submitted, Under Review, Evaluated
  ├── SubmissionDate, UpdatedAt
  └── 1 ←→ n SCORES

JUDGES (n)
  ├── JudgeId (PK)
  ├── UserId (FK → USERS)
  ├── CompetitionId (FK → COMPETITIONS)
  ├── Expertise
  ├── Priority
  ├── Status: Active, Inactive
  ├── AssignedDate, UpdatedAt
  └── 1 ←→ n SCORES

SCORING_CRITERIA (n)
  ├── CriteriaId (PK)
  ├── CompetitionId (FK → COMPETITIONS)
  ├── CriteriaName: Ý tưởng, Kỹ thuật, Thuyết trình...
  ├── MaxScore (Điểm tối đa)
  ├── Weight (Trọng số)
  ├── Order
  ├── CreatedAt, UpdatedAt
  └── 1 ←→ n SCORES

SCORES (n)
  ├── ScoreId (PK)
  ├── SubmissionId (FK → SUBMISSIONS)
  ├── JudgeId (FK → JUDGES)
  ├── CriteriaId (FK → SCORING_CRITERIA)
  ├── Score (Điểm chấm)
  ├── Comment
  └── ScoredDate, UpdatedAt

NOTIFICATIONS (n)
  ├── NotificationId (PK)
  ├── UserId (FK → USERS)
  ├── Title
  ├── Message
  ├── Type: Info, Warning, Success, Error
  ├── RelatedEntity & RelatedEntityId
  ├── IsRead
  └── CreatedAt, ReadAt
```

### 2.2 Chuẩn hóa Database

- **1NF (First Normal Form)**: Mỗi trường chứa giá trị nguyên tử
- **2NF (Second Normal Form)**: Tất cả non-key attributes phụ thuộc vào toàn bộ primary key
- **3NF (Third Normal Form)**: Không có transitive dependency
- **BCNF (Boyce-Codd Normal Form)**: Mỗi determinant là candidate key

### 2.3 Constraint Rules

```
KHÓA CHÍNH (Primary Keys):
- RoleId → Roles
- UserId → Users
- CompetitionId → Competitions
- RegistrationId → Registrations
- TeamId → Teams
- SubmissionId → Submissions
- JudgeId → Judges
- CriteriaId → ScoringCriteria
- ScoreId → Scores
- NotificationId → Notifications

KHÓA NGOẠI (Foreign Keys):
- Users.RoleId → Roles.RoleId (RESTRICT on delete)
- Registrations.UserId → Users.UserId (CASCADE)
- Registrations.CompetitionId → Competitions.CompetitionId (CASCADE)
- Registrations.TeamId → Teams.TeamId (SET NULL)
- Teams.CompetitionId → Competitions.CompetitionId (CASCADE)
- Teams.LeaderId → Users.UserId (RESTRICT)
- Submissions.CompetitionId → Competitions.CompetitionId (CASCADE)
- Submissions.RegistrationId → Registrations.RegistrationId (SET NULL)
- Submissions.TeamId → Teams.TeamId (SET NULL)
- Judges.UserId → Users.UserId (CASCADE)
- Judges.CompetitionId → Competitions.CompetitionId (CASCADE)
- ScoringCriteria.CompetitionId → Competitions.CompetitionId (CASCADE)
- Scores.SubmissionId → Submissions.SubmissionId (CASCADE)
- Scores.JudgeId → Judges.JudgeId (RESTRICT)
- Scores.CriteriaId → ScoringCriteria.CriteriaId (RESTRICT)
- Notifications.UserId → Users.UserId (CASCADE)

UNIQUE CONSTRAINTS:
- Users.Email (Unique)
- (Users.UserId, Competitions.CompetitionId) → Registrations (Unique)

INDEXES:
- Users.Email
- Competitions.Status
- Registrations.Status
- Submissions.CompetitionId, Status
- Scores.SubmissionId, JudgeId
```

---

## 3. QUYẾT ĐỊNH THIẾT KẾ CHÍNH

### 3.1 Quyền truy cập (Roles)

| Role | Chức năng |
|------|----------|
| **Admin** | Quản lý tất cả (Users, Competitions, Registrations, Judging) |
| **Student** | Đăng ký, Nộp bài, Xem kết quả |
| **Organizer** | Tạo/Chỉnh sửa cuộc thi, Xét duyệt đăng ký, Quản lý bài dự thi |
| **Judge** | Chấm điểm, Xem bài dự thi |
| **Lecturer** | Hướng dẫn học sinh, Xem kết quả |

### 3.2 Trạng thái (Status)

**Competition Status**: Draft → Active → Closed → Completed

**Registration Status**: Pending → Approved/Rejected/Withdrawn

**Submission Status**: Draft → Submitted → Under Review → Evaluated

### 3.3 Tính toán Điểm

```
TotalScorePerCriteria = Average(Judge1.Score, Judge2.Score, ...)
WeightedScore = TotalScorePerCriteria × Criteria.Weight
FinalScore = Sum(WeightedScore) / Sum(Weight)
```

### 3.4 Đặc biệt

- **Cá nhân hoặc Đội**: `Competitions.IsTeamBased` xác định loại hình
- **Nhiều vòng thi**: `Submissions.Round` cho phép tracking
- **Nhiều giám khảo**: `Scores` lưu score từ từng giám khảo riêng biệt
- **Soft Delete**: Không xóa vật lý (chỉnh sửa `IsActive` nếu cần)

---

## 4. MIGRATION & SEED DATA

Chạy lệnh sau để tạo database:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Dữ liệu khởi tạo (Roles) sẽ tự động được seed thông qua `ApplicationDbContext.OnModelCreating()`.

---

## 5. API ENDPOINTS (Dự kiến)

### Competitions
- `GET /api/competitions` - Danh sách
- `POST /api/competitions` - Tạo
- `GET /api/competitions/{id}` - Chi tiết
- `PUT /api/competitions/{id}` - Cập nhật
- `DELETE /api/competitions/{id}` - Xóa

### Registrations
- `POST /api/registrations` - Đăng ký
- `GET /api/registrations/user/{userId}` - Đăng ký của user
- `PUT /api/registrations/{id}/approve` - Xét duyệt

### Submissions
- `POST /api/submissions` - Nộp bài
- `GET /api/submissions/{id}` - Chi tiết
- `PUT /api/submissions/{id}` - Cập nhật

### Scores
- `POST /api/scores` - Chấm điểm
- `GET /api/scores/submission/{submissionId}` - Điểm của bài

### Notifications
- `GET /api/notifications` - Danh sách thông báo
- `PUT /api/notifications/{id}/read` - Đánh dấu đã đọc

---

## 6. SECURITY

- **Authentication**: JWT Token
- **Authorization**: Role-based (Policy-based)
- **Data Validation**: Fluent Validation hoặc Data Annotations
- **CORS**: Cấu hình cho frontend
- **Rate Limiting**: Prevent abuse
- **Encryption**: Password hashing (bcrypt/Argon2)

---

## 7. NEXT STEPS

1. ✅ Tạo Models
2. ✅ Cấu hình DbContext
3. ⏳ Tạo Migrations
4. ⏳ Seed dữ liệu test
5. ⏳ Tạo Repositories & Services
6. ⏳ Tạo Controllers & APIs
7. ⏳ Implement Authentication/Authorization
8. ⏳ Thêm Validation
9. ⏳ Error Handling
10. ⏳ Testing & Documentation
