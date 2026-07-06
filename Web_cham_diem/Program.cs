using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Web_cham_diem.Data;
using Web_cham_diem.Models;
using Web_cham_diem.Services;
using System.Globalization;

// Đảm bảo model binding dùng InvariantCulture để parse số thập phân (.) và ngày ISO 8601 đúng
// trong mọi locale của OS (tránh lỗi "The value '' is invalid" khi OS dùng vi-VN)
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

// QuestPDF Community license: miễn phí cho cá nhân/tổ chức có doanh thu < 1 triệu USD/năm
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Tăng giới hạn kích thước request để hỗ trợ upload ảnh và tài liệu (base64 ~33% overhead)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200 * 1024 * 1024; // 200MB
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.Limits.MaxRequestBodySize = 200 * 1024 * 1024; // 200MB
});

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<ICompetitionService, CompetitionService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<IGradingService, GradingService>();
builder.Services.AddScoped<IResultsService, ResultsService>();
builder.Services.AddScoped<INotificationsService, NotificationsService>();

// Add Authentication with Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login"; // 🌟 SỬA TẠI ĐÂY: Khớp chính xác với Controller Account của bạn
        options.LogoutPath = "/Account/Logout"; // 🌟 SỬA TẠI ĐÂY: Khớp với hàm Logout trong AccountController
        options.AccessDeniedPath = "/Home/Error"; // Hoặc trang báo lỗi quyền của bạn
        options.Cookie.Name = "AuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // allow HTTP in dev
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add authentication and authorization middleware (ORDER MATTERS!)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "competition-details",
    pattern: "Competitions/{id:int}",
    defaults: new { controller = "Competitive", action = "Details" });

app.MapControllerRoute(
    name: "competitions",
    pattern: "Competitions/{action=Index}/{id?}",
    defaults: new { controller = "Competitive" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

try
{
    await DbSeeder.SeedAsync(app);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Seed data thất bại: {Message}", ex.Message);
    if (ex.InnerException != null)
        logger.LogError("Inner: {Inner}", ex.InnerException.Message);
}

app.Run();