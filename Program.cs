using KabloStokTakipSistemi.Middlewares;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.Services; // Servis sınıflarını tanımak için
using Microsoft.EntityFrameworkCore;
using NLog.Web;
using AutoMapper;
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

    // HTTP Context (gerekirse NLog MDLC vb. için)
    builder.Services.AddHttpContextAccessor();

    // Servis katmanı (DI) kayıtları
    builder.Services.AddScoped<UserService>();
    builder.Services.AddScoped<CableService>();
    builder.Services.AddScoped<AlertService>();
    builder.Services.AddScoped<LogService>();
    builder.Services.AddScoped<IReportService, ReportService>();
    builder.Services.AddScoped<EmailService>();
    builder.Services.AddScoped<IEmployeeService, EmployeeService>();

    // JWT için bu kısıma Authentication/Authorization config eklenir

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
    
    app.UseAuthentication();  // Yapılacak JWT için
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
