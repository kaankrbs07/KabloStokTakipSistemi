using System.Reflection;
using KabloStokTakipSistemi.Configuration;
using KabloStokTakipSistemi.Data;
using KabloStokTakipSistemi.Middlewares;
using KabloStokTakipSistemi.Services.Implementations;
using KabloStokTakipSistemi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// Configuration
// -------------------------------------------------------
var configuration = builder.Configuration;

// DbContext (MSSQL)
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    var cs = configuration.GetConnectionString("DefaultConnection");
    opt.UseSqlServer(cs);
});

// AutoMapper (mevcut tüm profilleri tara)
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Controller + JSON
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    { 
        
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS 
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// SMTP Options bind + EmailService
builder.Services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailService, EmailService>();

// Domain servis kayıtları
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICableService, CableService>();
builder.Services.AddScoped<IStockMovementService, StockMovementService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IAlertService, AlertService>();

// builder.Services.AddAuthentication(options => { ... })
//     .AddJwtBearer(options => { ... });
// builder.Services.AddAuthorization();

// -------------------------------------------------------
// Build
// -------------------------------------------------------
var app = builder.Build();

// -------------------------------------------------------
// Middleware pipeline
// -------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Hata yakalama (ilklerde olsun ki hepsini kapsasın)
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// jwt gelince
// app.UseAuthentication();
app.UseAuthorization();

// DB session context middleware (kullanıcı id'sini MSSQL SESSION_CONTEXT'e yazar)
app.UseMiddleware<SessionContextMiddleware>();

app.MapControllers();

app.Run();
