using MySqlConnector;
using System.Data;
using Microsoft.AspNetCore.DataProtection;
using QuanLyNhaTro.Repositories;
using QuanLyNhaTro.Services;

var builder = WebApplication.CreateBuilder(args);

if (builder.Configuration.GetValue<bool>("UseEphemeralDataProtection"))
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
}

// ── MVC ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.Configure<TenantPhotoOptions>(
    builder.Configuration.GetSection(TenantPhotoOptions.SectionName));

// ── Database (Dapper + MySqlConnector) ───────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddTransient<IDbConnection>(_ =>
    new MySqlConnection(connectionString));

// ── Repositories ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<NhaRepository>();
builder.Services.AddScoped<PhongRepository>();
builder.Services.AddScoped<KhachThueRepository>();
builder.Services.AddScoped<KhachThueService>();
builder.Services.AddScoped<HopDongRepository>();
builder.Services.AddScoped<HopDongKhachThueRepository>();
builder.Services.AddScoped<CuTruService>();
builder.Services.AddScoped<HoaDonRepository>();
builder.Services.AddScoped<ChiTietHoaDonRepository>();
builder.Services.AddScoped<ThanhToanRepository>();
builder.Services.AddScoped<GiaoDichCocRepository>();
builder.Services.AddScoped<DichVuRepository>();
builder.Services.AddScoped<PhongDichVuRepository>();
builder.Services.AddScoped<HopDongDichVuRepository>();
builder.Services.AddScoped<ChiSoDienNuocRepository>();
builder.Services.AddScoped<ChiSoNgoaiHopDongRepository>();
builder.Services.AddScoped<KhoanPhatSinhHopDongRepository>();
builder.Services.AddScoped<KiemTraDuLieuRepository>();
builder.Services.AddScoped<LichSuThayDoiGiaRepository>();
builder.Services.AddScoped<LichSuHinhThucDichVuRepository>();
builder.Services.AddScoped<ThuChiRepository>();

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddScoped<HoaDonService>();
builder.Services.AddScoped<HopDongService>();
builder.Services.AddScoped<PhongService>();
builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<ChuyenPhongService>();
builder.Services.AddScoped<TraPhongService>();
builder.Services.AddScoped<GiaoDichCocService>();
builder.Services.AddScoped<CongNoSettlementService>();
builder.Services.AddScoped<GiaService>();
builder.Services.AddScoped<HoaDonSnapshotService>();
builder.Services.AddScoped<HinhThucDichVuService>();
builder.Services.AddScoped<ChiSoService>();
builder.Services.AddScoped<TenantPhotoStorage>();

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
