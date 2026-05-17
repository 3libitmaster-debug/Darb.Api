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
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using System.IO;
using System;

var builder = WebApplication.CreateBuilder(args);

#region 1. Firebase Admin SDK Initialization

var credentialPath = builder.Configuration["Firebase:CredentialFilePath"] ?? "firebase-service-account.json";
var absolutePath = Path.Combine(builder.Environment.ContentRootPath, credentialPath);

if (File.Exists(absolutePath))
{
  try
  {
    if (FirebaseApp.DefaultInstance == null)
    {
      FirebaseApp.Create(new AppOptions
      {
        Credential = GoogleCredential.FromFile(absolutePath)
      });
      Console.WriteLine("Firebase Admin SDK initialized successfully.");
    }
  }
  catch (Exception ex)
  {
    Console.WriteLine($"Error initializing Firebase Admin SDK: {ex.Message}");
  }
}
else
{
  Console.WriteLine($"Firebase credential file not found at: {absolutePath}. Simulation mode enabled.");
}

#endregion

#region 2. Global Culture & Localization Configuration

// Forces the system to use Gregorian calendar (Prevents Hijri year 47 display on certain system configurations)
var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

#endregion

#region 3. Services Configuration

// --- Controllers & JSON Serializer Configurations ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
      options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
      options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
      options.JsonSerializerOptions.Converters.Add(new TimeOnlyJsonConverter());
    });

// --- CORS Configuration (Essential for Flutter Mobile App Integration) ---
builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowAll", policy =>
  {
    policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
  });
});

// --- Database Connection with Automatic Server Resilience Retry Strategy ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));

// --- JWT Authentication Configuration ---
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

builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();

#endregion

#region 4. Dependency Injection (DI) Registrations

// --- Infrastructure & Repositories ---
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IRepository<Governorate>, Repository<Governorate>>();
builder.Services.AddScoped<IRepository<City>, Repository<City>>();
builder.Services.AddScoped<IRepository<Advertisement>, Repository<Advertisement>>();
builder.Services.AddScoped<IRepository<Company>, Repository<Company>>();
builder.Services.AddScoped<IRepository<Trip>, Repository<Trip>>();

// --- Domain Services ---
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

// --- Core Utility Services ---
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// --- Background Hosted Services ---
builder.Services.AddHostedService<DatabaseCleanupService>();

// --- Options Pattern Settings Mapping ---
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));

#endregion

#region 5. Swagger & API Documentation Settings

builder.Services.AddSwaggerGen(c =>
{
  c.SwaggerDoc("v1", new OpenApiInfo { Title = "Darb Web API", Version = "v1" });

  // JWT Bearer Security Definition
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
});

#endregion

#region 6. HTTP Request Pipeline (Middleware)

var app = builder.Build();

// Swagger Configuration (Configured for Hosted Production and IIS/Monster ASP compatibility)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
  // The "./" is critical for resolving paths on hosted subdirectories
  c.SwaggerEndpoint("./v1/swagger.json", "Darb API v1");
  c.RoutePrefix = "swagger";
});

// Middleware Executions Order: StaticFiles -> HttpsRedirection -> Cors -> Authentication -> Authorization -> MapControllers
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

#endregion