using ChoThueQuanAo.Data;
using ChoThueQuanAo.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. ĐĂNG KÝ CONTROLLERS VÀ VIEWS
builder.Services.AddControllersWithViews();

// 2. CẤU HÌNH DATABASE
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. ĐĂNG KÝ SERVICE
builder.Services.AddScoped<RentalContractService>();

// 4. CẤU HÌNH ĐĂNG NHẬP (COOKIE)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7); // Giữ đăng nhập 7 ngày
    });

builder.Services.AddAuthorization();

// 5. CẤU HÌNH SESSION
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// --- TỰ ĐỘNG CẬP NHẬT DATABASE (KHÔNG XÓA DỮ LIỆU) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Thay EnsureCreated bằng Migrate để quản lý dữ liệu tốt hơn nếu có dùng Migration
    // Nếu Vân chưa dùng Migration bao giờ thì dùng EnsureCreated cũng tạm ổn, 
    // nhưng TUYỆT ĐỐI không được mở dòng EnsureDeleted ra nhé.
    try {
        // db.Database.Migrate(); // Mở dòng này nếu bạn có dùng Add-Migration
        db.Database.EnsureCreated(); // Nếu chỉ làm đồ án đơn giản thì dùng dòng này là đủ
    } catch (Exception ex) {
        Console.WriteLine("Lỗi khởi tạo DB: " + ex.Message);
    }
}

// Cấu hình Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// THỨ TỰ QUAN TRỌNG
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();