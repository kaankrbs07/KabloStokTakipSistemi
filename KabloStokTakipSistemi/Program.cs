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

// -------- NLog --------
var nlogPath = Path.Combine(contentRoot, "Configuration", "nlog.config");
if (File.Exists(nlogPath))
    LogManager.Setup().LoadConfigurationFromFile(nlogPath);
else
    LogManager.Setup().LoadConfigurationFromAppSettings();

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// NLog'u etkinleştir
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// -------- DbContext --------
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var cs = configuration.GetConnectionString("DefaultConnection");
    opt.UseSqlServer(cs);
});

// -------- AutoMapper --------
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// -------- MVC + API --------
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -------- CORS --------
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader() 
        .AllowAnyMethod()
        .WithExposedHeaders("Authorization")); 
});

// -------- SMTP / Email --------
builder.Services.AddOptions<SmtpOptions>()
    .Bind(configuration.GetSection("Smtp"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddScoped<IEmailService, EmailService>();

// -------- Domain Servisleri --------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<ICableService, CableService>();
builder.Services.AddScoped<IStockMovementService, StockMovementService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ILogService, LogService>();

// -------- Auth / JWT --------
builder.Services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var jwt = configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt ayarlarÄ± eksik (appsettings).");

        opt.TokenValidationParameters = new TokenValidationParameters
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

        // Header yoksa çereze (cookie) bak sayfalar 401 yemesin
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var hasAuth = ctx.Request.Headers.ContainsKey("Authorization");
                if (!hasAuth)
                {
                    var tokenFromCookie = ctx.Request.Cookies["access_token"];
                    if (!string.IsNullOrEmpty(tokenFromCookie))
                    {
                        ctx.Token = tokenFromCookie;
                        ctx.HttpContext.Items["AuthSource"] = "Cookie";
                    }
                }
                else ctx.HttpContext.Items["AuthSource"] = "Header";

                ctx.Response.OnStarting(() =>
                {
                    if (ctx.HttpContext.Items.TryGetValue("AuthSource", out var src))
                        ctx.Response.Headers["X-Auth-Source"] = src.ToString();
                    return Task.CompletedTask;
                });
                return Task.CompletedTask;
            }
        };

    });

builder.Services.AddAuthorization();
builder.Services.AddResponseCompression();

var app = builder.Build();

// -------- Pipeline --------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseRouting();


// 1. Exception handling en başta
app.UseMiddleware<GlobalExceptionMiddleware>();

// 2. Kimlik doğrulama ve yetkilendirme
app.UseAuthentication();
app.UseAuthorization();

// 3. Logging middleware 
app.UseMiddleware<RequestResponseLoggingMiddleware>();

// 4. Session context (SQL SESSION_CONTEXT) 
app.UseMiddleware<SessionContextMiddleware>();

// -------- Rotalar --------
// MVC (conventional)
app.MapControllers();

// API (attribute routing)
app.MapControllers();

// NLog flush
// Buffer'daki loglar kaybolmasın
app.Lifetime.ApplicationStopped.Register(LogManager.Shutdown);

app.Run();
