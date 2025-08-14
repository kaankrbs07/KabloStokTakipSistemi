using System.Reflection;
using System.Text.Json.Serialization;
using KabloStokTakipSistemi.Configuration;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.Middlewares;
using KabloStokTakipSistemi.Services.Implementations;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web; // ✅ NLog.Web.AspNetCore

// -------------------------------------------------------
// NLog konfigürasyonunu yükle (Configuration/nlog.config varsa oradan, yoksa kök dizinden nlog.config)
// -------------------------------------------------------
var contentRoot = Directory.GetCurrentDirectory();
var customNLogPath = Path.Combine(contentRoot, "Configuration", "nlog.config");
if (File.Exists(customNLogPath))
{
    NLog.LogManager.Setup().LoadConfigurationFromFile(customNLogPath);
}
else
{
    // Kökte nlog.config varsa otomatik yüklenecek; ayrıca appsettings üzerinden de okunabilir.
    NLog.LogManager.Setup().LoadConfigurationFromAppSettings();
}

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// -------------------------------------------------------
// Logging: Tüm varsayılan sağlayıcıları temizleyip NLog'u ekle
// -------------------------------------------------------
builder.Logging.ClearProviders();     // Console, Debug vb. kapat
builder.Host.UseNLog();               // ✅ ILogger -> NLog yönlendirmesi

// -------------------------------------------------------
// Infrastructure
// -------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var cs = configuration.GetConnectionString("DefaultConnection");
    opt.UseSqlServer(cs);
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddOptions<SmtpOptions>()
    .Bind(configuration.GetSection("Smtp"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddScoped<IEmailService, EmailService>();

// Domain servisleri
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICableService, CableService>();
builder.Services.AddScoped<IStockMovementService, StockMovementService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IAlertService, AlertService>();

// JWT ileride eklenecek
// builder.Services.AddAuthentication(/* ... */).AddJwtBearer(/* ... */);
// builder.Services.AddAuthorization();

builder.Services.AddResponseCompression();

var app = builder.Build();

// -------------------------------------------------------
// Pipeline
// -------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global exception -> NLog'a gider
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors("AllowAll");

// app.UseAuthentication();
app.UseAuthorization();

// SESSION_CONTEXT yazımı (auth’tan sonra)
app.UseMiddleware<SessionContextMiddleware>();

app.MapControllers();

// NLog flush (uygulama kapanırken)
app.Lifetime.ApplicationStopped.Register(NLog.LogManager.Shutdown);

app.Run();

