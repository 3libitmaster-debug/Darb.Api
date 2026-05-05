using Darb.Api.DTOs.Base;
using Darb.Api.Models;
using Darb.Api.Repositories.Implementations;
using Darb.Api.Repository.Interfaces;
using Darb.Api.Services.Implementations;
using Darb.Api.Services.Implemention;
using Darb.Api.Services.Interfaces;
using Darb.Api.Services.BackgroundServices;
using darbWebApp.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Darb.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// --- 1. GLOBAL CULTURE CONFIGURATION ---
// Forces the system to use Gregorian calendar (Prevents Hijri year 47 display)
var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// --- 2. SERVICES CONFIGURATION ---

// Configure JSON to handle circular references (important for many-to-many relationships)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
    });

// CORS: Essential for Flutter/Mobile app integration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Database connection with automatic retry strategy for server resilience
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));

// JWT Authentication Settings
var secretKey = builder.Configuration["JwtSettings:Key"];
var issuer = builder.Configuration["JwtSettings:Issuer"];
var audience = builder.Configuration["JwtSettings:Audience"];

if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("CRITICAL: JWT Key is missing from appsettings.json.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Token Validation Failed: {context.Exception.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token is valid.");
            return Task.CompletedTask;
        }
    };
});

// --- 3. DEPENDENCY INJECTION (DI) ---
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>(); 
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IRepository<Governorate>, Repository<Governorate>>();
builder.Services.AddScoped<IRepository<City>, Repository<City>>();
builder.Services.AddScoped<IRepository<Advertisement>, Repository<Advertisement>>();
builder.Services.AddScoped<IPassengerService, PassengerService>();
builder.Services.AddScoped<IRepository<Company>, Repository<Company>>();
builder.Services.AddScoped<IRepository<Trip>, Repository<Trip>>();

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddHostedService<DatabaseCleanupService>();

builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));


builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();

// Swagger Documentation with JWT Security Scheme
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Darb Web API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Input your JWT token directly below."
    });

    

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
    c.EnableAnnotations();

    //c.OrderActionsBy((apiDesc) =>
    //{
    //    var methodOrder = new Dictionary<string, int>
    //    {
    //        { "GET", 1 },
    //        { "POST", 2 },
    //        { "PUT", 3 },
    //        { "DELETE", 4 }
    //    };
    //    return methodOrder.GetValueOrDefault(apiDesc.HttpMethod ?? string.Empty, 5).ToString();
    //});
});

var app = builder.Build();

// --- 4. MIDDLEWARE PIPELINE ---

// Fixed Swagger config for Monster ASP (Works outside Development)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // The "./" is critical for resolving paths on hosted subdirectories
    c.SwaggerEndpoint("./v1/swagger.json", "Darb API v1");
    c.RoutePrefix = "swagger";
});

// Middleware Order: StaticFiles -> Https -> Cors -> Auth
app.UseStaticFiles();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();