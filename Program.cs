using ChoThueQuanAo.Data;
using ChoThueQuanAo.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. ĐĂNG KÝ CONTROLLERS VÀ VIEWS
builder.Services.AddControllersWithViews();

// 2. CẤU HÌNH DATABASE (Fix lỗi mất dữ liệu)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. ĐĂNG KÝ SERVICE HẬU CẦN
builder.Services.AddScoped<RentalContractService>();

// 4. CẤU HÌNH ĐĂNG NHẬP (COOKIE)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddAuthorization();

// 5. CẤU HÌNH SESSION (Fix lỗi HttpOnly/IsEssential)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;   // Thêm .Cookie vào đây nè Vân
    options.Cookie.IsEssential = true; // Thêm .Cookie vào đây luôn
});

var app = builder.Build();

// --- TỰ ĐỘNG TẠO DATABASE NẾU CHƯA CÓ (GIỮ LẠI DỮ LIỆU CŨ) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // db.Database.EnsureDeleted(); // ĐÃ KHÓA: Để không bị xóa dữ liệu cũ
    db.Database.EnsureCreated();
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

// THỨ TỰ BẮT BUỘC: Session -> Auth -> Auth
app.UseSession();        // Chỉ để 1 dòng thôi nha
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();