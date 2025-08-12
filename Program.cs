using KabloStokTakipSistemi.Middlewares;
using KabloStokTakipSistemi.Data;
using Microsoft.EntityFrameworkCore;
using NLog.Web;
using KabloStokTakipSistemi.Services.Implementations;
using KabloStokTakipSistemi.Services.Interfaces;

NLog.LogManager.Setup().LoadConfigurationFromAppSettings();
var logger = NLog.LogManager.GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // NLog
    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
    builder.Host.UseNLog();

    // DbContext
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // AutoMapper: MappingProfiles klasöründeki tüm profilleri tara
    builder.Services.AddAutoMapper(typeof(Program).Assembly);

    // Swagger & Controller
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // HTTP Context (NLog MDLC vb. için)
    builder.Services.AddHttpContextAccessor();

    // Servis katmanı (DI) kayıtları
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<ICableService, CableService>();
    builder.Services.AddScoped<IAlertService, AlertService>();
    builder.Services.AddScoped<ILogService, LogService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IEmployeeService, EmployeeService>();
    builder.Services.AddScoped<IAdminService, AdminService>();
    builder.Services.AddScoped<IStockMovementService, StockMovementService>();
    builder.Services.AddScoped<IDepartmentService, DepartmentService>();

    var app = builder.Build();

    // Swagger
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Global hataları yakala (Error seviyesinde NLog'a düşer)
    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseHttpsRedirection();

    // Auth (JWT eklendiğinde aktif olacak)
    app.UseAuthentication();
    app.UseAuthorization();

    // SESSION_CONTEXT middleware (SP/trigger senaryoları için)
    app.UseMiddleware<SessionContextMiddleware>();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Program başlatılırken fatal bir hata oluştu");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}
