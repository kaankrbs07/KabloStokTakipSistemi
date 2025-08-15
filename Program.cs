using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using KabloStokTakipSistemi.Configuration;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.Middlewares;
using KabloStokTakipSistemi.Services.Implementations;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.Web;

var contentRoot = Directory.GetCurrentDirectory();

// NLog
var nlogPath1 = Path.Combine(contentRoot, "Configuration", "nlog.config");
if (File.Exists(nlogPath1))
    LogManager.Setup().LoadConfigurationFromFile(nlogPath1);
else
    LogManager.Setup().LoadConfigurationFromAppSettings();

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Logging -> NLog
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// DbContext
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var cs = configuration.GetConnectionString("DefaultConnection");
    opt.UseSqlServer(cs);
});

// AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// ⬇️ Burayı değiştiriyoruz: Views için MVC eklensin
builder.Services.AddControllersWithViews()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// SMTP + Email
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

// Auth/JWT
builder.Services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var jwt = configuration.GetSection("Jwt").Get<JwtOptions>()
                  ?? throw new InvalidOperationException("Jwt ayarları eksik (appsettings).");
        opt.TokenValidationParameters = new()
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddResponseCompression();

var app = builder.Build();

// Swagger (Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

// Middlewares
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

// SQL SESSION_CONTEXT (UserID yazımı) — Auth’tan sonra
app.UseMiddleware<SessionContextMiddleware>();

// ⬇️ Statik dosyalar (wwwroot) ve MVC route'ları
app.UseStaticFiles();

// API controller’lar (mevcut hali kalsın)
app.MapControllers();

// ⬇️ Views için default rota: "/" -> Home/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// NLog flush
app.Lifetime.ApplicationStopped.Register(LogManager.Shutdown);

app.Run();
