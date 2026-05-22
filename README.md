# 🎓 Hệ Thống Quản Lý Cuộc Thi Sinh Viên

## 📌 Tổng Quan

Đây là một hệ thống quản lý cuộc thi sinh viên toàn diện được xây dựng bằng **ASP.NET Core** với **Entity Framework Core** sử dụng mô hình **Code First**.

### Đặc điểm chính:
- ✅ **3 Module chính**: Quản lý cuộc thi, Quản lý bài dự thi & chấm điểm, Công bố kết quả
- ✅ **5 Loại người dùng**: Admin, Student, Organizer, Judge, Lecturer
- ✅ **Database schema chuẩn hóa**: Tuân theo 3NF/BCNF
- ✅ **10 Entities**: Roles, Users, Competitions, Registrations, Teams, Submissions, Judges, ScoringCriteria, Scores, Notifications
- ✅ **Hỗ trợ** cả thi cá nhân và thi đội
- ✅ **Chấm điểm** bởi nhiều giám khảo
- ✅ **Tính điểm** với trọng số

---

## 📊 Database Schema

### 10 Bảng Chính

```
Roles (5 vai trò)
├── Users (Người dùng)
│   ├── Registrations (Đăng ký)
│   ├── Judges (Giám khảo)
│   └── Notifications (Thông báo)
├── Competitions (Cuộc thi)
│   ├── Teams (Đội thi)
│   ├── Submissions (Bài dự thi)
│   └── ScoringCriteria (Tiêu chí chấm)
├── Scores (Điểm chấm)
└── [Relationships]
```

### Chuẩn Hóa Database

- **1NF**: Mỗi trường là giá trị nguyên tử
- **2NF**: Non-key attributes phụ thuộc vào toàn bộ primary key
- **3NF**: Không có transitive dependency
- **BCNF**: Mỗi determinant là candidate key

### Foreign Key Relationships

| From | To | Behavior |
|------|----|---------:|
| Users.RoleId | Roles.RoleId | RESTRICT |
| Registrations.UserId | Users.UserId | CASCADE |
| Registrations.CompetitionId | Competitions.CompetitionId | NO ACTION |
| Registrations.TeamId | Teams.TeamId | SET NULL |
| Teams.CompetitionId | Competitions.CompetitionId | NO ACTION |
| Teams.LeaderId | Users.UserId | RESTRICT |
| Submissions.CompetitionId | Competitions.CompetitionId | NO ACTION |
| Submissions.RegistrationId | Registrations.RegistrationId | SET NULL |
| Submissions.TeamId | Teams.TeamId | NO ACTION |
| Judges.UserId | Users.UserId | CASCADE |
| Judges.CompetitionId | Competitions.CompetitionId | CASCADE |
| ScoringCriteria.CompetitionId | Competitions.CompetitionId | CASCADE |
| Scores.SubmissionId | Submissions.SubmissionId | CASCADE |
| Scores.JudgeId | Judges.JudgeId | RESTRICT |
| Scores.CriteriaId | ScoringCriteria.CriteriaId | RESTRICT |
| Notifications.UserId | Users.UserId | CASCADE |

---

## 🏗️ Kiến Trúc Dự Án

```
Web_cham_diem/
├── Models/                          # Data Models
│   ├── Roles.cs
│   ├── Users.cs
│   ├── Competitions.cs
│   ├── Registrations.cs
│   ├── Teams.cs
│   ├── Submissions.cs
│   ├── Judges.cs
│   ├── ScoringCriteria.cs
│   ├── Scores.cs
│   ├── Notifications.cs
│   ├── ApplicationDbContext.cs      # DbContext
│   └── ErrorViewModel.cs
├── Migrations/                       # EF Migrations
│   ├── 20260522090328_InitialCreate.cs
│   └── ApplicationDbContextModelSnapshot.cs
├── Controllers/                      # (TBD) API Controllers
├── Services/                         # (TBD) Business Logic
├── Repositories/                     # (TBD) Data Access
├── Views/                            # Razor Pages
├── Program.cs                        # Cấu hình ứng dụng
├── appsettings.json                 # Cấu hình kết nối DB
└── Web_cham_diem.csproj            # Project file
```

---

## 🚀 Bắt Đầu

### Prerequisites
- .NET 8.0 SDK
- SQL Server (hoặc SQLite)
- Visual Studio 2022 / VS Code

### 1. Clone hoặc mở dự án
```bash
cd E:\web chuyen de\Web_cham_diem\Web_cham_diem
```

### 2. Cài đặt dependencies (nếu chưa có)
```bash
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
```

### 3. Build dự án
```bash
dotnet build
```

### 4. Database đã được khởi tạo
Database `Web_cham_diem` đã được tạo với tất cả bảng và relationships.

### 5. Xem dữ liệu Roles
```sql
SELECT * FROM Roles;
```

---

## 📈 Entity Relationships

### 1. Users - Roles (1:n)
```
1 Role → n Users
Admin có thể quản lý nhiều Users
```

### 2. Users - Registrations (1:n)
```
1 User → n Registrations
Một sinh viên có thể đăng ký nhiều cuộc thi
```

### 3. Users - Judges (1:n)
```
1 User → n Judges (JudgeAssignments)
Một giám khảo có thể được phân công cho nhiều cuộc thi
```

### 4. Competitions - Teams (1:n)
```
1 Competition → n Teams
Mỗi cuộc thi có nhiều đội
```

### 5. Competitions - Submissions (1:n)
```
1 Competition → n Submissions
Mỗi cuộc thi có nhiều bài dự thi
```

### 6. Competitions - ScoringCriteria (1:n)
```
1 Competition → n ScoringCriteria
Mỗi cuộc thi có nhiều tiêu chí chấm điểm
```

### 7. Teams - Registrations (1:n)
```
1 Team → n Registrations
Một đội có nhiều thành viên đăng ký
```

### 8. Registrations - Submissions (1:n)
```
1 Registration → n Submissions (hoặc 1 Team → n Submissions)
Mỗi đơn đăng ký/đội có thể nộp nhiều bài (nhiều vòng)
```

### 9. Submissions - Scores (1:n)
```
1 Submission → n Scores
Một bài dự thi được chấm bởi nhiều giám khảo
```

### 10. Judges - Scores (1:n)
```
1 Judge → n Scores
Một giám khảo chấm nhiều bài/tiêu chí
```

### 11. ScoringCriteria - Scores (1:n)
```
1 Criteria → n Scores
Một tiêu chí có nhiều điểm từ các giám khảo khác nhau
```

---

## 💾 Migrations

### Tạo Migration Mới
```bash
dotnet ef migrations add [MigrationName]
```

### Update Database
```bash
dotnet ef database update
```

### Xem Migrations
```bash
dotnet ef migrations list
```

### Revert Migration
```bash
dotnet ef database update [PreviousMigrationName]
dotnet ef migrations remove
```

---

## 📋 API Endpoints (Kế hoạch)

### Competitions
- `GET /api/competitions` - Danh sách cuộc thi
- `POST /api/competitions` - Tạo cuộc thi
- `GET /api/competitions/{id}` - Chi tiết cuộc thi
- `PUT /api/competitions/{id}` - Cập nhật cuộc thi
- `DELETE /api/competitions/{id}` - Xóa cuộc thi

### Registrations
- `POST /api/registrations` - Đăng ký
- `GET /api/registrations/user/{userId}` - Đơn đăng ký của user
- `PUT /api/registrations/{id}/approve` - Xét duyệt
- `PUT /api/registrations/{id}/reject` - Từ chối

### Submissions
- `POST /api/submissions` - Nộp bài
- `GET /api/submissions/{id}` - Chi tiết bài
- `PUT /api/submissions/{id}` - Cập nhật bài
- `GET /api/submissions/competition/{id}` - Bài của cuộc thi

### Scores
- `POST /api/scores` - Chấm điểm
- `GET /api/scores/submission/{id}` - Điểm bài dự thi
- `GET /api/scores/judge/{judgeId}` - Điểm của giám khảo
- `PUT /api/scores/{id}` - Cập nhật điểm

### Notifications
- `GET /api/notifications` - Danh sách thông báo
- `GET /api/notifications/unread` - Thông báo chưa đọc
- `PUT /api/notifications/{id}/read` - Đánh dấu đã đọc

---

## 🔐 Authentication & Authorization

### Roles

| Role | Chức năng |
|------|----------|
| **Admin** | Quản lý tất cả (Users, Competitions, Scores) |
| **Student** | Đăng ký, Nộp bài, Xem kết quả của mình |
| **Organizer** | Tạo cuộc thi, Xét duyệt đăng ký, Quản lý bài dự thi |
| **Judge** | Chấm điểm, Xem bài dự thi |
| **Lecturer** | Hướng dẫn sinh viên, Xem kết quả |

### Authorization Policy
```csharp
[Authorize(Roles = "Admin,Organizer")]
public async Task<IActionResult> ApproveRegistration(int id) { }
```

---

## 📊 Tính Toán Điểm

### Công Thức
```
1. Điểm trung bình mỗi tiêu chí:
   AvgCriteria = SUM(Judge1.Score, Judge2.Score, ...) / CountJudges

2. Điểm có trọng số:
   WeightedScore = AvgCriteria × Criteria.Weight

3. Điểm cuối cùng:
   FinalScore = SUM(WeightedScore) / SUM(Weight)
```

### Ví dụ
```
Tiêu chí 1: Ý tưởng (Weight: 1.0, MaxScore: 10)
  - Judge1: 8.5
  - Judge2: 9.0
  - AvgCriteria = (8.5 + 9.0) / 2 = 8.75

Tiêu chí 2: Kỹ thuật (Weight: 1.5, MaxScore: 10)
  - Judge1: 9.0
  - Judge2: 8.5
  - AvgCriteria = (9.0 + 8.5) / 2 = 8.75

FinalScore = (8.75×1.0 + 8.75×1.5) / (1.0 + 1.5)
           = (8.75 + 13.125) / 2.5
           = 21.875 / 2.5
           = 8.75
```

---

## 🛠️ Các Bước Tiếp Theo

### Ngắn hạn (Sprint 1)
- [ ] Tạo Repository Pattern
- [ ] Tạo Service Layer
- [ ] Implement Authentication (JWT)
- [ ] Tạo User Controller

### Trung hạn (Sprint 2)
- [ ] Tạo Competition Management
- [ ] Implement Registration System
- [ ] Tạo Submission Module
- [ ] Implement Scoring Engine

### Dài hạn (Sprint 3+)
- [ ] Dashboard & Reports
- [ ] Notification System (Email, SMS)
- [ ] Export PDF/Excel
- [ ] Unit Tests & Integration Tests

---

## 📝 Lưu Ý Quan Trọng

1. **Cascade Delete**: Được cấu hình cẩn thận để tránh data loss không mong muốn
2. **Decimal Precision**: Sử dụng `decimal(18,2)` cho tất cả scores
3. **Unique Constraints**: Email của Users phải unique
4. **Soft Delete**: Có thể implement trong tương lai thêm field `IsDeleted`
5. **Indexing**: Tất cả Foreign Keys tự động có indexes

---

## 📚 Tài Liệu Tham Khảo

- **SYSTEM_DESIGN.md** - Thiết kế hệ thống chi tiết
- **DEVELOPMENT_GUIDE.md** - Hướng dẫn phát triển từng module
- **Microsoft Docs** - https://docs.microsoft.com/en-us/ef/core/

---

## ✅ Checklist Hoàn Thành

- [x] Phân tích hệ thống
- [x] Thiết kế Database Schema
- [x] Tạo 10 Entities
- [x] Cấu hình DbContext
- [x] Tạo Migrations
- [x] Khởi tạo Database
- [x] Seed dữ liệu Roles
- [x] Build thành công
- [ ] Tạo Repositories
- [ ] Tạo Services
- [ ] Tạo Controllers
- [ ] Implement Authentication
- [ ] Tạo API Documentation

---

## 📞 Hỗ Trợ

Mọi câu hỏi hay vấn đề liên quan:
- Xem **SYSTEM_DESIGN.md** để hiểu design
- Xem **DEVELOPMENT_GUIDE.md** để hướng dẫn phát triển
- Check file migrations để xem schema

**Dự án được tạo ngày**: May 22, 2026  
**Target Framework**: .NET 8.0  
**Database**: SQL Server  
**ORM**: Entity Framework Core 8.0.0

---

**Happy Coding! 🚀**
